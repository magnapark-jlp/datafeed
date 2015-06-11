﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace JLPMPDev.Datafeed.Core
{
  public class Mailer
  {
    public static void SendMail(List<Attribute> list, Config config)
    {
      try
      {
        // initialize SmtpClient and MailMessage
        int smtpPort = config.SMTP.Port ?? SMTP.DefaultSMTPPort;
        SmtpClient client = new SmtpClient(config.SMTP.Host, smtpPort);
        MailMessage message = new MailMessage();
        
        message.IsBodyHtml = true;

        // mail from
        message.From = new MailAddress("datafeed@jlpmpdev.co.uk", "Datafeed");
        
        // mail to
        foreach (Email mail in config.Email)
        {
          MailAddress to = new MailAddress(mail.Address, mail.Name);
          message.To.Add(to);
        }

        // message subject and body
        string reportTitle = config.Report.Title ?? Report.DefaultReportTitle;
        string timeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        message.Subject = string.Format("{0}: {1}", reportTitle, timeNow);

        var groups = Attribute.BuildGroups(list);

        StringBuilder sb = new StringBuilder();
        using (StringWriter sw = new StringWriter(sb))
        {
          foreach (var group in groups)
          {
            sw.WriteLine("<h2>{0}</h2>", group.Key);
            sw.WriteLine("<ul>");
            foreach (var record in group)
            {
              string html = string.Format("<li>{0}: <strong>{1}</strong>", record.Name, record.Value);
              if (record.Description != string.Empty)
              {
                html += string.Format(" | <em>{0}</em>", record.Description);
              }

              sw.WriteLine(html);
            }

            sw.WriteLine("</ul>");
          }
        }

        // get last write time of file
        string feedRetrievalAt = config.Feed.RetrievalAt().ToString("dd/MM/yyyy HH:mm");
        string feedStatus;
        if (config.Feed.RetrievalAt() < DateTime.Now.AddMinutes(-30))
        {
          feedStatus = string.Format("Feed Status: <span class=\"old\">Out of date. [{0}]</span>", feedRetrievalAt);
        }
        else
        {
          feedStatus = "Feed Status: No problems found.";
        }

        string templatePath = config.Template.Path ?? Template.DefaultTemplatePath;
        using (StreamReader sr = new StreamReader(templatePath))
        {
          reportTitle = config.Report.Title ?? Report.DefaultReportTitle;
          message.Body = 
            sr.ReadToEnd().Replace("%main%", sb.ToString())
                          .Replace("%feedStatus%", feedStatus)
                          .Replace("%reportTitle%", reportTitle);
        }

        // send message
        client.Send(message);
      }

      // catches the exceptions, if any
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        Console.ReadKey();
      }
    }
  }
}
