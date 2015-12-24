using Couchbase.Lite;
using Couchbase.Lite.Listener.Tcp;
using Couchbase.Lite.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CouchbaseP2P_Blog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _replicateToAddress;
        public string ReplicateToAddress
        {
            get { return _replicateToAddress; }
            set
            {
                _replicateToAddress = value;
                NotifyPropertyChanged();
            }
        }

        private string _replicateToPort;
        public string ReplicateToPort
        {
            get { return _replicateToPort; }
            set
            {
                _replicateToPort = value;
                NotifyPropertyChanged();
            }
        }

        private string _listenOnPort;
        public string ListenOnPort
        {
            get { return _listenOnPort; }
            set
            {
                _listenOnPort = value;
                NotifyPropertyChanged();
            }
        }

        private string _isReplicating;
        public string IsReplicating
        {
            get { return _isReplicating; }
            set
            {
                _isReplicating = value;
                NotifyPropertyChanged();
            }
        }

        private string _isListening;
        public string IsListening
        {
            get { return _isListening; }
            set
            {
                _isListening = value;
                NotifyPropertyChanged();
            }
        }

        private string _docuemntId;
        public string DocumentId
        {
            get { return _docuemntId; }
            set
            {
                _docuemntId = value;
                NotifyPropertyChanged();
            }
        }

        private string _documentText;
        public string DocumentText
        {
            get { return _documentText; }
            set
            {
                _documentText = value;
                NotifyPropertyChanged();
            }
        }


        private Manager _manager;
        private Database _database;
        private CouchbaseLiteTcpListener _listener;

        private const string DB_NAME = "sampledb";
        private const string TAG = "sampledb";

        private List<Replication> _pulls;
        private List<Replication> _pushes;

        private DirectoryInfo _dbPath;

        private static ILogger Logger;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            ReplicateToAddress = "10.0.0.5";
            ReplicateToPort = "49840";
            ListenOnPort = "49840";
            IsReplicating = "Not Replicating";
            IsListening = "Not Listening";
            DocumentId = "SomeDocumentId";
            DocumentText = JObject.Parse(@"{'name': 'Roi Katz', 'Blogging':'Couchbase Lite Replication Works!' }").ToString();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            {
                Debug.WriteLine("Initializeing CouchbaseLite");
                try
                {
                    Log.SetDefaultLoggerWithLevel(SourceLevels.Verbose);

                    _dbPath = new DirectoryInfo(Environment.CurrentDirectory);
                    Log.I(TAG, "Creating manager in " + _dbPath);
                    Debug.WriteLine("Creating manager in " + _dbPath);

                    _manager = new Manager(_dbPath, ManagerOptions.Default);
                    Debug.WriteLine("Creating database " + DB_NAME);
                    Log.I(TAG, "Creating database " + DB_NAME);

                    _database = _manager.GetDatabase(DB_NAME);

                }
                catch (Exception ex)
                {
                    var msg = "Could not load database in path " + _dbPath.ToString();
                    MessageBox.Show(msg);
                }
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }

        private void StartListenerClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _listener = new CouchbaseLiteTcpListener(_manager, ushort.Parse(ListenOnPort), DB_NAME);
                _listener.Start();
                IsListening = "Listening";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StartReplcatingClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_pulls == null) _pulls = new List<Replication>();
                if (_pushes == null) _pushes = new List<Replication>();

                var pull = _database.CreatePullReplication(CreateSyncUri(ReplicateToAddress, int.Parse(ReplicateToPort), DB_NAME));
                var push = _database.CreatePushReplication(CreateSyncUri(ReplicateToAddress, int.Parse(ReplicateToPort), DB_NAME));

                pull.Continuous = true;
                push.Continuous = true;

                pull.Start();
                push.Start();

                _pulls.Add(pull);
                _pushes.Add(push);

                IsReplicating = "Replicaing!";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private Uri CreateSyncUri(string hostname, int port, string dbName)
        {
            Uri syncUri = null;
            string scheme = "http";

            try
            {
                var uriBuilder = new UriBuilder(scheme, hostname, port, dbName);
                syncUri = uriBuilder.Uri;
            }
            catch (UriFormatException e)
            {
                Debug.WriteLine(string.Format("{0}: Cannot create sync uri = {1}", dbName, e.Message));
            }
            return syncUri;
        }

        private void InsertDocumentClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentId))
            {
                MessageBox.Show("Please specify ID");
                return;
            }

            var document = _database.GetDocument(DocumentId);

            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(DocumentText);
            var revision = document.PutProperties(properties);

        }

        private void GetDocumentClick(object sender, RoutedEventArgs e)
        {
            var doc = _database.GetDocument(DocumentId);

            DocumentText = JsonConvert.SerializeObject(doc.Properties, Formatting.Indented);
        }
    }
}
