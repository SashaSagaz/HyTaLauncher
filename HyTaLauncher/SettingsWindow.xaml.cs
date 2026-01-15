using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using HyTaLauncher.Helpers;
using HyTaLauncher.Services;
using Microsoft.Win32;

namespace HyTaLauncher
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsManager _settingsManager;
        private readonly LocalizationService _localization;
        private string RussifierUrl => Config.RussifierUrl;
        private string OnlineFixUrl => Config.OnlineFixUrl;

        public SettingsWindow(SettingsManager settingsManager, LocalizationService localization)
        {
            FontHelper.Initialize();
            InitializeComponent();
            
            // Применяем шрифт
            if (FontHelper.CinzelFont != null)
            {
                FontFamily = FontHelper.CinzelFont;
            }
            
            _settingsManager = settingsManager;
            _localization = localization;
            LoadSettings();
            UpdateUI();
            CheckGameInstalled();
        }

        private void UpdateUI()
        {
            Title = _localization.Get("settings.title");
            GameDirLabel.Text = _localization.Get("settings.game_folder");
            //InfoText.Text = _localization.Get("settings.info");
            //InfoDescText.Text = _localization.Get("settings.info_desc");
            CancelBtn.Content = _localization.Get("settings.cancel");
            SaveBtn.Content = _localization.Get("settings.save");
            MirrorLabel.Text = _localization.Get("settings.mirror");
            UseMirrorText.Text = _localization.Get("settings.use_mirror");
            MirrorWarningText.Text = _localization.Get("settings.mirror_warning");
            RussifierLabel.Text = _localization.Get("settings.russifier");
            RussifierBtnText.Text = _localization.Get("settings.install_russifier");
            OnlineFixLabel.Text = _localization.Get("settings.onlinefix");
            OnlineFixBtnText.Text = _localization.Get("settings.install_onlinefix");
            OnlineFixWarningText.Text = _localization.Get("settings.onlinefix_warning");
        }

        private void CheckGameInstalled()
        {
            var gameDir = GetGameDirectory();
            var isInstalled = IsGameInstalled(gameDir);
            
            RussifierBtn.IsEnabled = isInstalled;
            OnlineFixBtn.IsEnabled = isInstalled;
            
            RussifierStatusText.Text = isInstalled 
                ? "" 
                : _localization.Get("settings.russifier_no_game");
            OnlineFixStatusText.Text = isInstalled 
                ? "" 
                : _localization.Get("settings.onlinefix_no_game");
            
            // Проверяем наличие бэкапов
            CheckBackupsAvailable();
        }

        private void CheckBackupsAvailable()
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HyTaLauncher", "backups"
            );
            
            RussifierRestoreBtn.Visibility = Directory.Exists(Path.Combine(backupDir, "russifier")) 
                ? Visibility.Visible : Visibility.Collapsed;
            OnlineFixRestoreBtn.Visibility = Directory.Exists(Path.Combine(backupDir, "onlinefix")) 
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private string GetGameDirectory()
        {
            var settings = _settingsManager.Load();
            return string.IsNullOrEmpty(settings.GameDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hytale")
                : settings.GameDirectory;
        }

        private bool IsGameInstalled(string gameDir)
        {
            var installBase = Path.Combine(gameDir, "install");
            if (!Directory.Exists(installBase))
                return false;

            // Проверяем все ветки: release, pre-release, beta, alpha
            var branches = new[] { "release", "pre-release", "beta", "alpha" };
            foreach (var branch in branches)
            {
                var branchDir = Path.Combine(installBase, branch, "package", "game");
                if (!Directory.Exists(branchDir))
                    continue;

                // Проверяем есть ли хотя бы одна версия с HytaleClient.exe
                foreach (var dir in Directory.GetDirectories(branchDir))
                {
                    var clientPath = Path.Combine(dir, "Client", "HytaleClient.exe");
                    if (File.Exists(clientPath))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Получает все папки версий игры из всех веток
        /// </summary>
        private List<string> GetAllGameVersionDirs(string gameDir)
        {
            var versionDirs = new List<string>();
            var installBase = Path.Combine(gameDir, "install");
            
            if (!Directory.Exists(installBase))
                return versionDirs;

            var branches = new[] { "release", "pre-release", "beta", "alpha" };
            foreach (var branch in branches)
            {
                var branchDir = Path.Combine(installBase, branch, "package", "game");
                if (!Directory.Exists(branchDir))
                    continue;

                foreach (var dir in Directory.GetDirectories(branchDir))
                {
                    var clientPath = Path.Combine(dir, "Client", "HytaleClient.exe");
                    if (File.Exists(clientPath))
                        versionDirs.Add(dir);
                }
            }
            return versionDirs;
        }

        private void LoadSettings()
        {
            var settings = _settingsManager.Load();
            
            var defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Hytale"
            );
            GameDirTextBox.Text = string.IsNullOrEmpty(settings.GameDirectory) 
                ? defaultDir 
                : settings.GameDirectory;
            
            UseMirrorCheckBox.IsChecked = settings.UseMirror;
            MirrorWarningText.Visibility = settings.UseMirror ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UseMirrorCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = UseMirrorCheckBox.IsChecked == true;
            MirrorWarningText.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            
            if (isChecked)
            {
                MessageBox.Show(
                    _localization.Get("settings.mirror_confirm"),
                    _localization.Get("settings.mirror"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = _localization.Get("settings.select_folder")
            };

            if (dialog.ShowDialog() == true)
            {
                GameDirTextBox.Text = dialog.FolderName;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = _settingsManager.Load();
            settings.GameDirectory = GameDirTextBox.Text;
            settings.UseMirror = UseMirrorCheckBox.IsChecked == true;
            _settingsManager.Save(settings);
            
            MessageBox.Show(_localization.Get("settings.saved"), 
                _localization.Get("settings.success"), 
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private async void RussifierButton_Click(object sender, RoutedEventArgs e)
        {
            RussifierBtn.IsEnabled = false;
            RussifierStatusText.Text = _localization.Get("settings.russifier_downloading");
            RussifierStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                
                if (versionDirs.Count == 0)
                {
                    throw new Exception("No game versions found");
                }
                
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HyTaLauncher", "cache"
                );
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HyTaLauncher", "backups", "russifier"
                );
                Directory.CreateDirectory(cacheDir);

                var zipPath = Path.Combine(cacheDir, "ru.zip");
                var extractPath = Path.Combine(cacheDir, "ru_temp");

                // Скачиваем архив
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 YaBrowser/25.12.0.0 Safari/537.36");
                
                var response = await httpClient.GetAsync(RussifierUrl);
                response.EnsureSuccessStatusCode();
                
                await using (var fs = new FileStream(zipPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                RussifierStatusText.Text = _localization.Get("settings.russifier_installing");

                // Распаковываем
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Находим папку Client в архиве
                var clientSourceDir = Path.Combine(extractPath, "Client");
                if (!Directory.Exists(clientSourceDir))
                {
                    var dirs = Directory.GetDirectories(extractPath);
                    if (dirs.Length > 0)
                    {
                        clientSourceDir = Path.Combine(dirs[0], "Client");
                    }
                }

                if (!Directory.Exists(clientSourceDir))
                {
                    throw new Exception("Client folder not found in archive");
                }

                // Получаем список файлов для бэкапа
                var filesToBackup = GetFilesRecursive(clientSourceDir)
                    .Select(f => f.Substring(clientSourceDir.Length + 1))
                    .ToList();

                int installedCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    var clientDestDir = Path.Combine(versionDir, "Client");
                    if (Directory.Exists(clientDestDir))
                    {
                        // Делаем бэкап только первой версии (файлы одинаковые)
                        if (installedCount == 0)
                        {
                            BackupFiles(clientDestDir, backupDir, filesToBackup);
                        }
                        
                        CopyDirectory(clientSourceDir, clientDestDir);
                        installedCount++;
                    }
                }

                // Очистка
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                RussifierStatusText.Text = string.Format(_localization.Get("settings.russifier_done"), installedCount);
                RussifierStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2e, 0xa0, 0x43));
                
                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                RussifierStatusText.Text = $"{_localization.Get("settings.russifier_error")}: {ex.Message}";
                RussifierStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xcc, 0x33, 0x33));
            }
            finally
            {
                RussifierBtn.IsEnabled = true;
            }
        }

        private List<string> GetFilesRecursive(string dir)
        {
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(dir));
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                files.AddRange(GetFilesRecursive(subDir));
            }
            return files;
        }

        private void BackupFiles(string sourceDir, string backupDir, List<string> relativePaths)
        {
            // Очищаем старый бэкап
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);
            
            Directory.CreateDirectory(backupDir);
            
            foreach (var relativePath in relativePaths)
            {
                var sourceFile = Path.Combine(sourceDir, relativePath);
                var backupFile = Path.Combine(backupDir, relativePath);
                
                if (File.Exists(sourceFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
                    File.Copy(sourceFile, backupFile, true);
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private async void OnlineFixButton_Click(object sender, RoutedEventArgs e)
        {
            OnlineFixBtn.IsEnabled = false;
            OnlineFixStatusText.Text = _localization.Get("settings.onlinefix_downloading");
            OnlineFixStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                
                if (versionDirs.Count == 0)
                {
                    throw new Exception("No game versions found");
                }
                
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HyTaLauncher", "cache"
                );
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HyTaLauncher", "backups", "onlinefix"
                );
                Directory.CreateDirectory(cacheDir);

                var zipPath = Path.Combine(cacheDir, "online.zip");
                var extractPath = Path.Combine(cacheDir, "online_temp");

                // Скачиваем архив
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 YaBrowser/25.12.0.0 Safari/537.36");
                
                var response = await httpClient.GetAsync(OnlineFixUrl);
                response.EnsureSuccessStatusCode();
                
                await using (var fs = new FileStream(zipPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                OnlineFixStatusText.Text = _localization.Get("settings.onlinefix_installing");

                // Распаковываем
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Получаем список файлов для бэкапа
                var filesToBackup = GetFilesRecursive(extractPath)
                    .Select(f => f.Substring(extractPath.Length + 1))
                    .ToList();

                // Копируем содержимое в каждую версию игры
                int installedCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    // Делаем бэкап только первой версии
                    if (installedCount == 0)
                    {
                        BackupFiles(versionDir, backupDir, filesToBackup);
                    }
                    
                    CopyDirectory(extractPath, versionDir);
                    installedCount++;
                }

                // Очистка
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                OnlineFixStatusText.Text = string.Format(_localization.Get("settings.onlinefix_done"), installedCount);
                OnlineFixStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2e, 0xa0, 0x43));
                
                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                OnlineFixStatusText.Text = $"{_localization.Get("settings.onlinefix_error")}: {ex.Message}";
                OnlineFixStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xcc, 0x33, 0x33));
            }
            finally
            {
                OnlineFixBtn.IsEnabled = true;
            }
        }

        private void RussifierRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreBackup("russifier", RussifierStatusText);
        }

        private void OnlineFixRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreBackup("onlinefix", OnlineFixStatusText);
        }

        private void RestoreBackup(string backupName, System.Windows.Controls.TextBlock statusText)
        {
            try
            {
                var gameDir = GetGameDirectory();
                var versionDirs = GetAllGameVersionDirs(gameDir);
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HyTaLauncher", "backups", backupName
                );

                if (!Directory.Exists(backupDir))
                {
                    statusText.Text = "No backup found";
                    return;
                }

                var result = MessageBox.Show(
                    _localization.Get("settings.restore_confirm"),
                    _localization.Get("settings.restore_title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                    return;

                int restoredCount = 0;
                foreach (var versionDir in versionDirs)
                {
                    CopyDirectory(backupDir, versionDir);
                    restoredCount++;
                }

                // Удаляем бэкап после восстановления
                Directory.Delete(backupDir, true);
                
                statusText.Text = string.Format(_localization.Get("settings.restore_done"), restoredCount);
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2e, 0xa0, 0x43));
                
                CheckBackupsAvailable();
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xcc, 0x33, 0x33));
            }
        }
    }
}
