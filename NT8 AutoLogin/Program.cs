using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace NT8AutoLogin
{
    public class Program
    {
        public const string DefaultNinjaExePath = @"C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe";
        private const int ProcessStartTimeoutMs = 30000;
        private const int WindowReadyTimeoutMs = 30000;
        private static readonly string[] LoginButtonNames = { "Log In", "Login", "Sign In", "Sign in" };

        private sealed class LoginElements
        {
            public AutomationElement UserNameEdit { get; set; }
            public AutomationElement PasswordEdit { get; set; }
            public AutomationElement LoginButton { get; set; }
        }

        private static string GetAppDataFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NT8 AutoLogin");
        }

        private static string GetCredentialPath()
        {
            return Path.Combine(GetAppDataFolder(), "credentials.dat");
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(GetAppDataFolder(), "settings.ini");
        }

        /// <summary>Deletes locally saved sign-in credentials so the next Priority Launch prompts again.</summary>
        public static void ClearSavedCredentials()
        {
            string path = GetCredentialPath();
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void SaveCredentials(string username, string password)
        {
            string path = GetCredentialPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            byte[] usernameBytes = Encoding.UTF8.GetBytes(username ?? string.Empty);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

            byte[] encryptedUsername = ProtectedData.Protect(usernameBytes, null, DataProtectionScope.CurrentUser);
            byte[] encryptedPassword = ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);

            using (var fs = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(encryptedUsername.Length);
                writer.Write(encryptedUsername);
                writer.Write(encryptedPassword.Length);
                writer.Write(encryptedPassword);
            }
        }

        public static bool TryLoadPreferredNinjaExePath(out string ninjaExe)
        {
            ninjaExe = string.Empty;
            string path = GetSettingsPath();
            if (!File.Exists(path))
                return false;

            try
            {
                const string prefix = "NinjaExePath=";
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine == null ? string.Empty : rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                        continue;

                    if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string value = line.Substring(prefix.Length).Trim();
                    if (string.IsNullOrEmpty(value))
                        return false;

                    ninjaExe = value;
                    return true;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        public static string LoadPreferredNinjaExePathOrDefault()
        {
            string ninjaExe;
            return TryLoadPreferredNinjaExePath(out ninjaExe) ? ninjaExe : DefaultNinjaExePath;
        }

        public static void SavePreferredNinjaExePath(string ninjaExe)
        {
            if (string.IsNullOrWhiteSpace(ninjaExe))
                throw new InvalidOperationException("Please provide a valid NinjaTrader main executable path (NinjaTrader.exe).");

            string path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "NinjaExePath=" + ninjaExe.Trim() + Environment.NewLine, Encoding.UTF8);
        }

        public static bool TryLoadCredentials(out string username, out string password)
        {
            username = string.Empty;
            password = string.Empty;
            string path = GetCredentialPath();
            if (!File.Exists(path))
                return false;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                using (var reader = new BinaryReader(fs))
                {
                    int usernameLen = reader.ReadInt32();
                    byte[] encryptedUsername = reader.ReadBytes(usernameLen);
                    int passwordLen = reader.ReadInt32();
                    byte[] encryptedPassword = reader.ReadBytes(passwordLen);

                    if (encryptedUsername.Length != usernameLen || encryptedPassword.Length != passwordLen)
                        return false;

                    byte[] usernameBytes = ProtectedData.Unprotect(encryptedUsername, null, DataProtectionScope.CurrentUser);
                    byte[] passwordBytes = ProtectedData.Unprotect(encryptedPassword, null, DataProtectionScope.CurrentUser);

                    username = Encoding.UTF8.GetString(usernameBytes);
                    password = Encoding.UTF8.GetString(passwordBytes);
                }
                return true;
            }
            catch (IOException)
            {
                TryDeleteCredentialFile(path);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                TryDeleteCredentialFile(path);
                return false;
            }
            catch (CryptographicException)
            {
                TryDeleteCredentialFile(path);
                return false;
            }
            catch (ArgumentException)
            {
                TryDeleteCredentialFile(path);
                return false;
            }
        }

        private static void CleanNinjaFolders(string[] folders, Action<string> log)
        {
            string ntDocDir = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8"));
            Action<string> write = log ?? (_ => { });

            foreach (string folder in folders)
            {
                string path = Path.GetFullPath(Path.Combine(ntDocDir, folder));
                EnsurePathInsideRoot(ntDocDir, path);
                write("Deleting and recreating directory: " + folder);
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        write("Deleted directory: " + folder);
                    }

                    Directory.CreateDirectory(path);
                    write("Recreated empty directory: " + folder);
                }
                catch (Exception ex)
                {
                    throw new IOException("Failed to process directory: " + folder + ". Reason: " + ex.Message, ex);
                }
            }
        }

        public static void RunNinjaClean(Action<string> log)
        {
            if (IsNinjaTraderRunning())
                throw new InvalidOperationException("NinjaTrader is already running. Please close it before cleaning data.");

            CleanNinjaFolders(new[]
            {
                "cache",
                Path.Combine("db", "cache"),
                Path.Combine("db", "day"),
                Path.Combine("db", "minute"),
                Path.Combine("db", "tick"),
                "tmp",
                "trace",
                "log"
            }, log);
        }

        public static void LaunchNinjaTrader(string userName, string passwd, string ninjaExe)
        {
            ValidateNinjaExecutable(ninjaExe);

            if (string.IsNullOrWhiteSpace(passwd))
                throw new InvalidOperationException("Password cannot be empty.");

            var startInfo = new ProcessStartInfo(ninjaExe) { UseShellExecute = true };
            Process ninjaProc = Process.Start(startInfo);
            if (ninjaProc == null)
                throw new InvalidOperationException("Unable to launch NinjaTrader.");

            WaitForProcessReady(ninjaProc, ProcessStartTimeoutMs);

            try { ninjaProc.PriorityClass = ProcessPriorityClass.High; } catch { }

            AutomationElement loginWindow = WaitForLoginWindow(ninjaProc, WindowReadyTimeoutMs);
            LoginElements elements = WaitForLoginElements(loginWindow, WindowReadyTimeoutMs);

            SetEditValue(elements.PasswordEdit, passwd, "password field");

            if (!string.IsNullOrWhiteSpace(userName) && elements.UserNameEdit != null)
                SetEditValue(elements.UserNameEdit, userName, "username field");

            InvokeLoginButton(elements.LoginButton);
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                if (args.Length == 1 && string.Equals(args[0], "--reset", StringComparison.OrdinalIgnoreCase))
                    ClearSavedCredentials();

                // Command line: NT8 AutoLogin.exe "username" "password" [optional exe path]
                if (args.Length >= 2)
                {
                    string userName = args[0];
                    string passwd = args[1];
                    string ninjaExe = args.Length >= 3 ? args[2] : DefaultNinjaExePath;
                    ValidateNinjaExecutable(ninjaExe);
                    RunNinjaClean(_ => { });
                    LaunchNinjaTrader(userName, passwd, ninjaExe);
                    return;
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                if (args.Length >= 2)
                {
                    try { Console.Error.WriteLine(ex.Message); } catch { }
                    return;
                }

                MessageBox.Show(ex.Message, "NT8 AutoLogin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void EnsurePathInsideRoot(string rootPath, string candidatePath)
        {
            string normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!candidatePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Detected an unexpected directory path. Cleanup was aborted.");
        }

        public static bool IsNinjaTraderRunning()
        {
            return Process.GetProcessesByName("NinjaTrader").Length > 0;
        }

        private static void ValidateNinjaExecutable(string ninjaExe)
        {
            if (string.IsNullOrWhiteSpace(ninjaExe))
                throw new InvalidOperationException("Please provide a valid NinjaTrader main executable path (NinjaTrader.exe).");

            if (!File.Exists(ninjaExe))
                throw new FileNotFoundException("NinjaTrader.exe was not found. Please verify the path.", ninjaExe);
        }

        private static void WaitForProcessReady(Process process, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                process.Refresh();
                if (process.HasExited)
                    throw new InvalidOperationException("NinjaTrader exited immediately after launch. Auto sign-in cannot continue.");

                try
                {
                    if (process.WaitForInputIdle(500))
                        return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException("Timed out while waiting for NinjaTrader to become interactive.");
        }

        private static AutomationElement WaitForLoginWindow(Process process, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                process.Refresh();
                if (process.HasExited)
                    throw new InvalidOperationException("NinjaTrader exited before auto sign-in could continue.");

                IntPtr handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        AutomationElement root = AutomationElement.FromHandle(handle);
                        if (root != null)
                            return root;
                    }
                    catch (ElementNotAvailableException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException("Timed out while waiting for the NinjaTrader sign-in window.");
        }

        private static LoginElements WaitForLoginElements(AutomationElement loginWindow, int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    LoginElements elements = FindLoginElements(loginWindow);
                    if (elements.PasswordEdit != null && elements.LoginButton != null)
                        return elements;
                }
                catch (ElementNotAvailableException)
                {
                }

                Thread.Sleep(100);
            }

            throw new TimeoutException("Unable to identify the password field or sign-in button in the NinjaTrader sign-in window.");
        }

        private static LoginElements FindLoginElements(AutomationElement loginWindow)
        {
            var visibleEdits = FindFocusableElements(loginWindow, ControlType.Edit)
                .OrderBy(GetElementTop)
                .ThenBy(GetElementLeft)
                .ToList();

            var visibleButtons = FindFocusableElements(loginWindow, ControlType.Button)
                .OrderBy(GetElementTop)
                .ThenBy(GetElementLeft)
                .ToList();

            AutomationElement passwordEdit = visibleEdits.LastOrDefault();
            AutomationElement userNameEdit = visibleEdits.Count > 1 ? visibleEdits.First() : null;
            AutomationElement loginButton = FindLoginButton(visibleButtons, passwordEdit);

            return new LoginElements
            {
                UserNameEdit = userNameEdit,
                PasswordEdit = passwordEdit,
                LoginButton = loginButton,
            };
        }

        private static IEnumerable<AutomationElement> FindFocusableElements(AutomationElement root, ControlType controlType)
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType),
                new PropertyCondition(AutomationElement.IsEnabledProperty, true));

            return root.FindAll(TreeScope.Descendants, condition)
                .Cast<AutomationElement>()
                .Where(IsUsableElement);
        }

        private static AutomationElement FindLoginButton(IEnumerable<AutomationElement> buttons, AutomationElement passwordEdit)
        {
            var buttonList = buttons.ToList();
            foreach (string name in LoginButtonNames)
            {
                AutomationElement exact = buttonList.FirstOrDefault(b =>
                    string.Equals(b.Current.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            foreach (string name in LoginButtonNames)
            {
                AutomationElement partial = buttonList.FirstOrDefault(b =>
                    !string.IsNullOrWhiteSpace(b.Current.Name) &&
                    b.Current.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (partial != null)
                    return partial;
            }

            if (passwordEdit != null)
            {
                double passwordBottom = GetElementBottom(passwordEdit);
                AutomationElement belowPassword = buttonList.FirstOrDefault(b => GetElementTop(b) >= passwordBottom - 1);
                if (belowPassword != null)
                    return belowPassword;
            }

            return buttonList.FirstOrDefault();
        }

        private static bool IsUsableElement(AutomationElement element)
        {
            try
            {
                if (element.Current.IsOffscreen)
                    return false;

                System.Windows.Rect rect = element.Current.BoundingRectangle;
                return rect.Width > 1 && rect.Height > 1;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        private static double GetElementTop(AutomationElement element)
        {
            try { return element.Current.BoundingRectangle.Top; } catch { return double.MaxValue; }
        }

        private static double GetElementLeft(AutomationElement element)
        {
            try { return element.Current.BoundingRectangle.Left; } catch { return double.MaxValue; }
        }

        private static double GetElementBottom(AutomationElement element)
        {
            try { return element.Current.BoundingRectangle.Bottom; } catch { return double.MaxValue; }
        }

        private static void SetEditValue(AutomationElement element, string value, string elementName)
        {
            if (element == null)
                throw new InvalidOperationException("Unable to find the " + elementName + ".");

            object valuePatternObj;
            if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePatternObj))
                throw new InvalidOperationException("The " + elementName + " does not support ValuePattern, so it cannot be filled automatically.");

            var valuePattern = (ValuePattern)valuePatternObj;
            valuePattern.SetValue(value ?? string.Empty);
        }

        private static void InvokeLoginButton(AutomationElement button)
        {
            if (button == null)
                throw new InvalidOperationException("Unable to find the sign-in button.");

            object invokePatternObj;
            if (!button.TryGetCurrentPattern(InvokePattern.Pattern, out invokePatternObj))
                throw new InvalidOperationException("The sign-in button does not support InvokePattern, so it cannot be triggered automatically.");

            ((InvokePattern)invokePatternObj).Invoke();
        }

        private static void TryDeleteCredentialFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
    }
}
