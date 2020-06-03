using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vocup.Util;

namespace Vocup.Controls
{
    [DefaultEvent("FileSelected")]
    public partial class FileTreeView : UserControl
    {
        private const int ImgRoot = 0;
        private const int ImgRootSelected = 0;
        private const int ImgDir = 0;
        private const int ImgDirSelected = 0;
        private const int ImgFile = 2;
        private const int ImgFileSelected = 2;

        private Size _imageScalingBaseSize = new Size(16, 16);
        private string _rootPath = "";
        private bool interceptEvents;
        private SizeF scalingFactor = new SizeF(1F, 1F);

        public FileTreeView()
        {
            InitializeComponent();
            MainTreeView.PathSeparator = Path.DirectorySeparatorChar.ToString();
        }

        [DefaultValue(typeof(Size), "16,16")]
        public Size ImageScalingBaseSize
        {
            get => _imageScalingBaseSize;
            set
            {
                _imageScalingBaseSize = value;
                ScaleImageList();
            }
        }

        [DefaultValue("*.*")] public string FileFilter { get; set; } = "*.*";

        [DefaultValue(false)] public bool ShowHiddenFiles { get; set; } = false;

        [DefaultValue("")]
        public string RootPath
        {
            get => _rootPath;
            set
            {
                MainWatcher.EnableRaisingEvents = false;
                MainTreeView.BeginUpdate();
                MainTreeView.Nodes.Clear();
                var info = new DirectoryInfo(value);
                var node = new TreeNode
                {
                    Text = info.Name,
                    Tag = info,
                    ImageIndex = ImgRoot,
                    SelectedImageIndex = ImgRootSelected
                };
                MainTreeView.Nodes.Add(node);
                LoadNodes(node);
                MainTreeView.EndUpdate();
                _rootPath = value;
                MainWatcher.Path = value;
                MainWatcher.EnableRaisingEvents = true;
            }
        }

        [DefaultValue("")]
        public string SelectedPath
        {
            get => (MainTreeView.SelectedNode?.Tag as FileInfo)?.FullName;
            set
            {
                if (value == null) value = "";
                if (value != SelectedPath)
                {
                    interceptEvents = true;
                    MainTreeView.SelectedNode = GetNode(value);
                    interceptEvents = false;
                }
            }
        }

        [DefaultValue(false)]
        public bool BrowseButtonVisible
        {
            get => BrowseButton.Visible;
            set => BrowseButton.Visible = value;
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public event EventHandler<FileSelectedEventArgs> FileSelected;

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public event EventHandler BrowseClick;

        protected virtual void OnFileSelected(FileSelectedEventArgs e)
        {
            if (!interceptEvents) FileSelected?.Invoke(this, e);
        }

        protected virtual void OnBrowseClick(EventArgs e)
        {
            if (!interceptEvents) BrowseClick?.Invoke(this, e);
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            scalingFactor = scalingFactor.Multiply(factor);
            ScaleImageList();
            base.ScaleControl(factor, specified);
        }

        private void ScaleImageList()
        {
            var old = MainTreeView.ImageList;
            MainTreeView.ImageList =
                IconImageList.Scale(_imageScalingBaseSize.Multiply(scalingFactor).Rectify().Round());
            old?.Dispose();
        }

        private void LoadNodes(TreeNode root)
        {
            root.Nodes.Clear();
            var rootInfo = (DirectoryInfo) root.Tag;
            try
            {
                var directories = rootInfo.GetDirectories();
                var files = rootInfo.GetFiles(FileFilter);
                foreach (var directory in directories
                    .Where(x => ShowHiddenFiles || !x.Attributes.HasFlag(FileAttributes.Hidden)))
                    root.Nodes.Add(new TreeNode
                    {
                        Tag = directory,
                        Text = directory.Name,
                        ImageIndex = ImgDir,
                        SelectedImageIndex = ImgDirSelected
                    });
                foreach (var file in files
                    .Where(x => ShowHiddenFiles || !x.Attributes.HasFlag(FileAttributes.Hidden)))
                    root.Nodes.Add(new TreeNode
                    {
                        Tag = file,
                        Text = Path.GetFileNameWithoutExtension(file.FullName),
                        ImageIndex = ImgFile,
                        SelectedImageIndex = ImgFileSelected
                    });
            }
            catch (UnauthorizedAccessException)
            {
            } // Ignore contents of folders with restricted access
        }

        private TreeNode LoadNode(TreeNode parent, string path)
        {
            TreeNode result = null;

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                var info = new DirectoryInfo(path);
                parent.Nodes.Add(result = new TreeNode
                {
                    Tag = info,
                    Text = info.Name,
                    ImageIndex = 1,
                    SelectedImageIndex = 1
                });
                LoadNodes(result);
            }
            else if (PatternMatcher.StrictMatchPattern(FileFilter, path))
            {
                var info = new FileInfo(path);
                parent.Nodes.Add(result = new TreeNode
                {
                    Tag = info,
                    Text = Path.GetFileNameWithoutExtension(info.FullName),
                    ImageIndex = 2,
                    SelectedImageIndex = 2
                });
            }

            return result;
        }

        private TreeNode GetNode(string path)
        {
            if (!path.ToLower().Contains(_rootPath.ToLower()))
                return null;
            var relativePath = path.Substring(_rootPath.Length);

            var names = relativePath.Split(new[] {Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);

            if (MainTreeView.Nodes.Count == 0)
                return null;

            var currentNode = MainTreeView.Nodes[0];

            foreach (var name in names)
            {
                currentNode = currentNode.Nodes.Cast<TreeNode>().Where(x => GetName(x) == name).FirstOrDefault();
                if (currentNode == null)
                    return currentNode;
            }

            return currentNode;
        }

        private string GetName(TreeNode node)
        {
            if (node.Tag is DirectoryInfo directory)
                return directory.Name;
            if (node.Tag is FileInfo file)
                return file.Name;
            return null;
        }

        private void SafeRemoveNode(TreeNode node)
        {
            if (node != null)
            {
                if (SelectedPath == (node.Tag as FileInfo)?.FullName)
                    SelectedPath = null; // prevent from automatically loading another file
                node.Remove();
            }
        }

        private void MainTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            foreach (TreeNode node in e.Node.Nodes)
                if (node.Tag is DirectoryInfo)
                    LoadNodes(node);
        }

        private void MainTreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            foreach (TreeNode node in e.Node.Nodes) node.Nodes.Clear();
        }

        private void MainTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is FileInfo) OnFileSelected(new FileSelectedEventArgs(((FileInfo) e.Node.Tag).FullName));
        }

        private void MainWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var root = GetNode(Path.GetDirectoryName(e.FullPath));
            LoadNode(root, e.FullPath);
        }

        private void MainWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var node = GetNode(e.FullPath);
            SafeRemoveNode(node); // null check because no FileFilter is applied to the watcher
        }

        private void MainWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var old = GetNode(e.OldFullPath);
            SafeRemoveNode(old); // null check because user can change file ending to match FileFilter
            var root = GetNode(Path.GetDirectoryName(e.FullPath));
            LoadNode(root, e.FullPath);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            OnBrowseClick(e);
        }
    }
}