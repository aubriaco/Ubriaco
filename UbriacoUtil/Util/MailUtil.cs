/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/



using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Ubriaco.Util
{
    public class MailUtil
    {
        public static List<string> Blacklist = new List<string>();
        public static string? TplContent = null;
        public static string? TplPath = null;

        public static string? SMTPMailer, SMTPUsername, SMTPPassword, SMTPHost, BaseURL, FooterHTML;
        public static int SMTPPort = 465;
        public static bool SMTPSecure = true;
        public static byte[]? LogoFileData = null;
        public static string? LogoDataURL = null;

        public class MailConfig
        {
            public string host { get; set; } = string.Empty;
            public string mailer { get; set; } = string.Empty;
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public int port { get; set; } = 465;
        }

        public static void LoadConfig(string configFile)
        {
            var config = JsonConvert.DeserializeObject<MailConfig>(File.ReadAllText(configFile));
            if (config == null)
                throw new Exception("Failed to load MailUtil config.");
            Init(config.host, config.username, config.password, config.mailer);
        }

        public static void Init(string host, string username, string password, string mailer, int port = 587)
        {
            SMTPHost = host;
            SMTPMailer = mailer;
            SMTPUsername = username;
            SMTPPassword = password;
            SMTPPort = port;
            SMTPSecure = true;
            if (File.Exists("const/email.html"))
                TplContent = File.ReadAllText("const/email.html");
            else
                TplContent = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""x-apple-disable-message-reformatting"">
</head>
<body>
    <div>%(BODY)</div>
    <p><a href=""%(LINK_URL)"">%(LINK_TEXT)</a></p>
</body>
</html>";
            if (File.Exists("const/logo.png"))
            {
                var data = File.ReadAllBytes("const/logo.png");
                LogoFileData = data;
                LogoDataURL = "data:image/png;base64," + Convert.ToBase64String(data);
            }
            else
                Console.WriteLine("MailUtil: Failed to load logo.");
        }


        public static bool EmailIsOK(string email)
        {
            email = email.ToLower().Trim();
            if (!email.Contains('@'))
                return false;
            if (email.Contains('+'))
                return false;
            if (email.IndexOf('@') == email.Length)
                return false;

            var domain = email.Substring(email.IndexOf('@') + 1);

            if (Blacklist.Contains(domain))
                return false;

            return true;
        }

        public static bool EmailIsOK(string email, bool checkBlackList = true)
        {
            email = email.ToLower().Trim();
            if (!email.Contains('@'))
                return false;
            if (email.Contains('+'))
                return false;
            if (email.IndexOf('@') == email.Length)
                return false;

            var domain = email.Substring(email.IndexOf('@') + 1);

            if (checkBlackList)
            {
                if (Blacklist.Contains(domain))
                    return false;
            }

            return true;
        }

        public static void LoadBlacklist(string fileName = "const/email-blacklist.txt")
        {
            try
            {
                Blacklist.Clear();
                using (var file = File.OpenText(fileName))
                {
                    string? line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        line = line.Trim().ToLower().Replace("\r", "").Replace("\n", "");
                        Blacklist.Add(line);
                        Console.WriteLine("LoadBlacklist: Added: " + line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadBlacklist: Failed to load: " + ex.Message);
            }
        }

        public static bool Send(string to, string subject, string body, string? link = null, string? linkText = null, List<Attachment>? attachments = null, string? replyTo = null, string? mailer = null)
        {
            if (TplContent == null || string.IsNullOrEmpty(SMTPMailer) || string.IsNullOrEmpty(SMTPHost))
                throw new Exception("MailUtil: Not configured.");
            if (mailer == null)
                mailer = SMTPMailer;
            MailMessage mail = new MailMessage(mailer, to);
            SmtpClient client = new SmtpClient();
            client.Port = SMTPPort;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            NetworkCredential creds = new NetworkCredential(SMTPUsername, SMTPPassword);
            client.Credentials = creds;
            client.Host = SMTPHost;
            client.EnableSsl = SMTPSecure;
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.HeadersEncoding = Encoding.UTF8;
            if (replyTo != null)
                mail.ReplyToList.Add(replyTo);

            if (FooterHTML != null)
                body += FooterHTML;

            string content = TplContent;

            content = content.Replace("%(SUBJECT)", subject).Replace("%(BODY)", body);

            if (link != null)
                content = content.Replace("%(LINK_URL)", link).Replace("%(LINK_TEXT)", linkText);
            else
            {
                content = Regex.Replace(content, @"<!--BEGIN:BUTTON-->((?:\r|\n|.)*?)<!--END:BUTTON-->", "");
            }

            if (attachments != null)
            {
                foreach (var i in attachments)
                {
                    mail.Attachments.Add(i);
                }
            }

            MemoryStream? logoFileStream = null;

            if (LogoFileData != null)
            {
                logoFileStream = new MemoryStream(LogoFileData);
                var att = new Attachment(logoFileStream, "logo.png", "image/png");
                att.ContentId = "logo";
                mail.Attachments.Add(att);
                content = content.Replace("%(LOGO)", "cid:logo");
            }
            else
            {
                content = Regex.Replace(content, @"<!--BEGIN:HEADING-->((?:\r|\n|.)*?)<!--END:HEADING-->", "");
            }

            mail.Body = content;


            client.Send(mail);

            if (logoFileStream != null)
                logoFileStream.Dispose();

            return true;
        }


        public static async Task<bool> SendAsync(string to, string subject, string body, string? link = null, string? linkText = null, List<Attachment>? attachments = null, string? replyTo = null, string? mailer = null,
            string? appUrl = null)
        {
            if (TplContent == null || string.IsNullOrEmpty(SMTPMailer) || string.IsNullOrEmpty(SMTPHost))
                throw new Exception("MailUtil: Not configured.");
            if (mailer == null)
                mailer = SMTPMailer;
            MailMessage mail = new MailMessage(mailer, to);
            SmtpClient client = new SmtpClient();
            client.Port = SMTPPort;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            NetworkCredential creds = new NetworkCredential(SMTPUsername, SMTPPassword);
            client.Credentials = creds;
            client.Host = SMTPHost;
            client.EnableSsl = SMTPSecure;
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.HeadersEncoding = Encoding.UTF8;
            if (replyTo != null)
                mail.ReplyToList.Add(replyTo);

            if (FooterHTML != null)
                body += FooterHTML;

            string content = TplContent;

            if (appUrl != null)
                content = content.Replace("%(APPURL)", appUrl);

            content = content.Replace("%(SUBJECT)", subject).Replace("%(BODY)", body);

            if (link != null)
                content = content.Replace("%(LINK_URL)", link).Replace("%(LINK_TEXT)", linkText);
            else
            {
                content = Regex.Replace(content, @"<!--BEGIN:BUTTON-->((?:\r|\n|.)*?)<!--END:BUTTON-->", "");
            }

            if (attachments != null)
            {
                foreach (var i in attachments)
                {
                    mail.Attachments.Add(i);
                }
            }

            MemoryStream? logoFileStream = null;

            if (LogoFileData != null)
            {
                logoFileStream = new MemoryStream(LogoFileData);
                var att = new Attachment(logoFileStream, "logo.png", "image/png");
                att.ContentId = "logo";
                mail.Attachments.Add(att);
                content = content.Replace("%(LOGO)", "cid:logo");
            }
            else
            {
                content = Regex.Replace(content, @"<!--BEGIN:HEADING-->((?:\r|\n|.)*?)<!--END:HEADING-->", "");
            }

            mail.Body = content;


            await client.SendMailAsync(mail);

            if (logoFileStream != null)
                logoFileStream.Dispose();

            return true;
        }


    }
}
