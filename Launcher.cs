// PS HELP - przenosny plik EXE: program w srodku, rozpakowanie do %LOCALAPPDATA%\PS HELP\exe i start bez okna konsoli.
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

[assembly: AssemblyTitle("PS HELP")]
[assembly: AssemblyProduct("PS HELP")]
[assembly: AssemblyDescription("PS HELP - komendy PowerShell w jednym oknie")]
[assembly: AssemblyCompany("MRGREGOR85")]
[assembly: AssemblyCopyright("MRGREGOR85")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Launcher
{
    [STAThread]
    static int Main()
    {
        try
        {
            byte[] payload;
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using (var ms = new MemoryStream()) { s.CopyTo(ms); payload = ms.ToArray(); }

            string ver;
            using (var sha = SHA256.Create()) ver = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").Substring(0, 12).ToLowerInvariant();

            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PS HELP", "exe");
            string dir = Path.Combine(root, ver);
            if (!File.Exists(Path.Combine(dir, ".ok")))
            {
                string tmp = dir + ".tmp" + Process.GetCurrentProcess().Id;
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
                using (var zip = new ZipArchive(new MemoryStream(payload), ZipArchiveMode.Read))
                {
                    foreach (var e in zip.Entries)
                    {
                        if (string.IsNullOrEmpty(e.Name)) continue;
                        string target = Path.Combine(tmp, e.FullName.Replace('/', '\\'));
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        using (var src = e.Open()) using (var dst = File.Create(target)) src.CopyTo(dst);
                    }
                }
                File.WriteAllText(Path.Combine(tmp, ".ok"), ver);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.Move(tmp, dir);
            }
            // Stare wersje po aktualizacji pliku EXE.
            try
            {
                foreach (var old in Directory.GetDirectories(root))
                    if (!string.Equals(Path.GetFileName(old), ver, StringComparison.OrdinalIgnoreCase) && !old.Contains(".tmp"))
                        try { Directory.Delete(old, true); } catch { }
            }
            catch { }

            // Skrot "PS HELP" na pulpicie (dawniej do PS HELP.vbs) wskazuje teraz ten plik EXE.
            try
            {
                string exe = Assembly.GetExecutingAssembly().Location;
                string lnkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "PS HELP.lnk");
                if (File.Exists(lnkPath))
                {
                    dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                    dynamic lnk = sh.CreateShortcut(lnkPath);
                    if (!string.Equals((string)lnk.TargetPath, exe, StringComparison.OrdinalIgnoreCase))
                    {
                        lnk.TargetPath = exe; lnk.Arguments = ""; lnk.WorkingDirectory = Path.GetDirectoryName(exe);
                        lnk.IconLocation = exe + ",0"; lnk.Description = "PS HELP"; lnk.Save();
                    }
                }
            }
            catch { }

            string ps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            if (!File.Exists(ps)) ps = "powershell.exe";
            var psi = new ProcessStartInfo(ps,
                "-NoLogo -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + Path.Combine(dir, @"app\launch.ps1") + "\"");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.WorkingDirectory = dir;
            Process.Start(psi);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PS HELP - startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
