using System.Drawing.Drawing2D;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;
using System.Security.Cryptography;
using System.Text;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Net.Sockets;
using System.Net;
using NLog;
using NLog.Config;
using System.IO;
using System.ComponentModel.DataAnnotations.Schema;
using static HL7MessageGenerator.Main;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Web;
using System.Text.RegularExpressions;


namespace HL7MessageGenerator
{
    public partial class history : MaterialForm
    {
       /* private readonly HL7MessageCache _messageCache;*/


        /*public history(HL7MessageCache messageCache)
        {
            InitializeComponent();
            var messages = messageCache.GetMessages();
            /*
            listBox1.Items.Clear();
            foreach (var message in messages)
            {
                listBox1.Items.Add(message);
            }*/
            /*using (var fileStream = File.Create("messages.html"))
            {
                using (var writer = XmlWriter.Create(fileStream))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("html");
                    writer.WriteStartElement("body");

                    foreach (string message in messages)
                    {
                        string encodedMessage = Regex.Replace(message, @"\n", @"<br />");
                        writer.WriteStartElement("p");
                        writer.WriteString(encodedMessage);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement(); // body
                    writer.WriteEndElement(); // html
                    writer.WriteEndDocument();
                }
                webView21.NavigateToString(File.ReadAllText("messages.html"));

            }

        }*/

        /*private void history_Load(object sender, EventArgs e)
        {
            List<string> messages = _messageCache.GetMessages().ToList(); // Fixed the return type of _messageCache.GetMessages()

            // Create a DataTable to hold the message history
            DataTable dt = new DataTable();

            // Add two columns: MessageId and MessageText
            dt.Columns.Add("MessageId", typeof(string));
            dt.Columns.Add("MessageText", typeof(string));

            foreach (string message in messages)
            {
                DataRow row = dt.NewRow();
                // Add the MessageId and MessageText to the row
                row["MessageId"] = "Unknown"; // Replace with actual MessageId value
                row["MessageText"] = message; // Replace with actual MessageText value

                dt.Rows.Add(row);
            }

            listBox1.DataSource = dt;
        }*/


}
}
