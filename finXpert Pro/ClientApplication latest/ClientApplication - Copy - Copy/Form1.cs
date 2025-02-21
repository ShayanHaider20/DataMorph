using Newtonsoft.Json.Linq;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
using System.Diagnostics;
using Bunifu.UI.WinForms;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Configuration;

namespace ClientApplication
{
    public partial class Form1 : Form
    {
        public static int mouseclick = 1;
        TcpClient client = null;
        NetworkStream ns = null;
        StreamReader sr = null;
        TcpListener server = null;
        private string selectedRequestFilePath;
        private string selectedResponseFilePath;
        public string responseContent;
        char delimiter = '|';
        public static bool executefix = true;
        public static bool executexml = true;
        public static bool executejson = true;
        public static bool executeiso = true;
        public static bool executedelimited = true;

        public static bool executefixreq = true;
        public static bool executexmlreq = true;
        public static bool executejsonreq = true;
        public static bool executeisoreq = true;
        public static bool executedelimitedreq = true;

        public static string upd = "";
        public static int num = 1;
        private Socket socketforclients;
        private static int[] fix = { 5, 5, 5, 5, 5, 5, 5, 5 };
        private static int totalfixlength = 33;
        private static string fixedlengthtemp;
        private static string fixedlengthtempreq;
        HttpListener listener;
        static string url;
        static HttpListenerContext context;
        HttpListenerRequest request;
        HttpListenerResponse response;
        StringBuilder clientMessage;
        string[] fieldNamesArray = { };
        string[] fieldValuesArray = { };
        StreamWriter sw;
        SemaphoreSlim semaphore = new SemaphoreSlim(0);
        private int fields;
        private static int auto;

        public Form1()
        {
            InitializeComponent();
            PopulateRequestDropdown();
            PopulateFiles();

        }


        private void PopulateRequestDropdown()
        {
            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
            string[] requestFiles = Directory.GetFiles(filepath, "*.req.txt");

            foreach (string filePath in requestFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                requestDropdown.Items.Add(fileName);
            }
        }
        private void PopulateFiles()
        {
            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
            string[] Files = Directory.GetFiles(filepath, "*.txt");

            foreach (string filePath in Files)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                FileOpen.Items.Add(fileName);
            }
        }


        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Hide();
            protocolSelectionToolStripMenuItem.Enabled = false;

            for (int i = 0; i < 10; i++)
            {
                editfields.Items.Add(i + 1);
            }
            label8.Hide();
            label10.Hide();
            sendjson.Hide();
            sendxml.Hide();
            executexml = true;
            executejson = true;
            executeiso = true;
            executefix = true;
            executedelimited = true;

            executefixreq = true;
            executexmlreq = true;
            executejsonreq = true;
            executeisoreq = true;
            executedelimitedreq = true;
            AutoResponse.Checked = true;
            Selectall.Hide();
            button6.Hide();
            UpdatedResponsebtn.Hide();
            checkedListBox1.Hide();
        }

        private void clientToolStripMenuItem_Click(object sender, EventArgs e) //client click
        {
            button1.Hide();
        }

        private void serverToolStripMenuItem_Click(object sender, EventArgs e) //server click
        {
            button1.Show();
        }
        private bool TryGetValidIPAndPort(out string ip, out int port) //Validates IP and Port 
        {

            string ip_pattern = @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$";

            while (true)
            {

                ip = txtIp.Text;
                try
                {
                    port = Convert.ToInt32(txtPort.Text);
                }// Default value for port
                catch (System.FormatException fe)
                {
                    //MessageBox.Show(fe.Message);
                }

                if (string.IsNullOrWhiteSpace(ip) ||
                    !Regex.IsMatch(ip, ip_pattern) || !int.TryParse(txtPort.Text, out port) || port <= 0 || port > 65535)
                {
                    DialogResult result = MessageBox.Show("Invalid IP or port entered. Please enter valid IP or port .", "Try Again?", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Retry)
                    {
                        string Ipinput = Interaction.InputBox("Enter IP Address:", "Enter IP", txtIp.Text);
                        Invoke(new Action(() => txtIp.Text = Ipinput));
                        try
                        {
                            int portInput = Int32.Parse(Interaction.InputBox("Enter port number:", "Enter Port", txtPort.Text));

                            Invoke(new Action(() => txtPort.Text = portInput.ToString()));

                            if (int.TryParse(portInput.ToString(), out port) && Regex.IsMatch(Ipinput, ip_pattern) && port >= 0 && port < 65535)
                            {
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        catch (FormatException fe1)
                        {
                            //MessageBox.Show(fe1.Message);
                            continue;
                        }
                    }
                    else //cancel button pressed
                    {
                        DialogResult close = MessageBox.Show("Do you want to close the server?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (close == DialogResult.Yes)
                        {
                            Environment.Exit(0);
                        }
                        else
                        {
                            continue; //If No is pressed
                        }
                    }

                }
                else
                {
                    break;
                }
            }
            return true; // Both IP and port are valid

        }


        private async void button1_Click_1(object sender, EventArgs e) //Start listening button, it opens server and waits for client to connect
        {
            await Task.Run(() =>
            {
                button1.Enabled = true;
                while (true)
                {
                    string ip;
                    int port;
                    if (TryGetValidIPAndPort(out ip, out port))
                    {
                        StartButton().Wait();
                        break; // Exit the loop and start the server
                    }
                }
            });
        }


        public async Task StartButton()
        {
            string Ip = txtIp.Text;
            int Port = Convert.ToInt32(txtPort.Text);
            string socket = "";
            try
            {
                //Invoke command used to switch thread control
                server = new TcpListener(IPAddress.Parse(Ip), Port);
                Invoke(new Action(() => server.Start()));
                Invoke(new Action(() => button1.Enabled = false));
                Invoke(new Action(() => label12.Show()));
                Invoke(new Action(() => label12.Text = "Server started. Listening for clients..."));
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => MessageBox.Show("Error starting the server: " + ex.Message)));
            }
            try
            {

                socketforclients = await server.AcceptSocketAsync();
                Invoke(new Action(() => label12.Text = "Client Connected to server at address" + socketforclients.RemoteEndPoint.ToString().Split(':')[0] + ":" + Port));
                while (true)
                {
                    if (server == null || socketforclients == null || !server.Server.IsBound)
                    {
                        Invoke(new Action(() => MessageBox.Show("Server is not started or not bound to a socket. Please check your input and try again.")));

                    }
                    StringBuilder clientMessage = new StringBuilder();//this string holds request content from client sent through networkstream
                    Invoke(new Action(() => sendResponse()));
                    Invoke(new Action(() => clientMessage.Clear()));
                    using (NetworkStream ns = new NetworkStream(socketforclients))
                    using (StreamReader sr = new StreamReader(ns, Encoding.UTF8, true, 4096, true))
                    {
                        char[] buffer = new char[4096];
                        int bytesRead;
                        Array.Clear(buffer, 0, buffer.Length);

                        while ((bytesRead = await sr.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            string line = new string(buffer, 0, bytesRead);
                            clientMessage.Append(line);
                            Invoke(new Action(() => richTextBox1.Text = line));
                            break;
                        }
                    }
                    Invoke(new Action(() => checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty)));
                    Invoke(new Action(() => socket = richTextBox1.Text));
                    Invoke(new Action(() => UpdatedResponsebtn.Show()));
                    Invoke(new Action(() => checkedListBox1.Items.Clear()));
                    Invoke(new Action(() => richTextBox2.Clear()));
                    Invoke(new Action(() => responseContent = null));
                    Invoke(new Action(() => ResponseCheckboxList.Items.Clear()));
                    string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                    string[] responseFiles = Directory.GetFiles(filepath, "*.res.txt");
                    string retpath = "";
                    Invoke(new Action(() => Selectall.Text = "Select All"));
                    foreach (string filePath in responseFiles)
                    {
                        string file = ConfigurationManager.AppSettings["FilePath"].ToString();
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string responseFilePath = Path.Combine(file, fileName + ".txt");
                        string responseContent = File.ReadAllText(responseFilePath);

                        //If request sent is in valid format, (Check isvalidresponse function for more details)
                        if (MessageHandler.IsValidResponse(socket, responseContent, responseFilePath))
                        {
                            if (!ISO.IsISO8583Format(socket))
                            {
                                Invoke(new Action(() => ResponseCheckboxList.Items.Add(fileName)));
                                string respcont = File.ReadAllText(filePath);
                                retpath = filePath;
                                fixedlengthtemp = respcont;
                                Invoke(new Action(() => richTextBox2.Text = respcont));
                                Invoke(new Action(() => checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty)));
                                break;
                            }
                            else
                            {
                                Invoke(new Action(() => ResponseCheckboxList.Items.Add(fileName)));
                                Invoke(new Action(() => richTextBox2.Text = getstringinreqformatiso(responseFilePath)));
                                Invoke(new Action(() => checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty)));
                                break;
                            }

                        }
                        //If valid request is not present 
                        else
                        {
                            Invoke(new Action(() => richTextBox2.Text = "Error in finding either request or response file"));
                            continue;
                        }

                    }
                }
            }
            catch (Exception e9)
            {
                MessageBox.Show(e9.Message);
            }
            finally
            {

                if (socketforclients != null)
                {
                    //socketforclients.Close();
                }

                if (server != null)
                {
                    //server.Stop();
                }
            }

        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void txtPort_TextChanged(object sender, EventArgs e) //port
        {

        }

        private void txtIp_TextChanged(object sender, EventArgs e) //ip
        {

        }

        //Open protocol selection menu only when there is some data in response
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox2.Text != null)
            {
                protocolSelectionToolStripMenuItem.Enabled = true;
                protocolSelectionToolStripMenuItem.HideDropDown();
            }
            else
            {
                protocolSelectionToolStripMenuItem.Enabled = false;
            }

            if (listener != null && AutoResponse.Checked == true)
            {
                AutoResponse_CheckedChanged(null, EventArgs.Empty);
            }
        }

        //Information about creators of simulator
        private void aBoutUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            protocolSelectionToolStripMenuItem.Enabled = false;
            FileOpen.Enabled = false;
            requestDropdown.Enabled = false;
            selectTypeToolStripMenuItem.Enabled = false;
            back.Visible = true;
            richTextBox3.Visible = true;
            foreach (Control c in this.Controls)
            {
                if (c is RichTextBox || c is Label || c is Button || c is TextBox || c is ListBox || c is CheckBox)
                {
                    editfields.Visible = false;
                    c.Visible = false;
                }
            }
            back.Visible = true;
            richTextBox3.Visible = true;
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            int desiredWidth = (100 * screenWidth) / 100;
            int desiredHeight = (400 * screenHeight) / 100;
            richTextBox3.Width = desiredWidth;
            richTextBox3.Height = desiredHeight;
            richTextBox3.Text = "Welcome to our " +
                "software product, FinXpert Pro developed with utmost dedication and expertise by" +
                " Daniyal and\nShayan, two ambitious students at Fast University. As interns at iPath Ltd Pvt, we " +
                "strived to create a cutting-edge\nsolution that meets the dynamic demands of today's technology landscape.\n\n" +
                "Our product is meticulously crafted using the powerful development environment, Visual" +
                "Studio, and designed on the\nWindows Form platform." +
                " Leveraging the capabilities of C# programming language and the robust .NET framework,\n" +
                "we have achieved a user-friendly and feature-rich software experience." +
                "Our simulator ensures seamless and\nreal-time handling of incoming requests and delivers contextually relevant responses.   ";

        }

        private void jsonToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        //Server starts and waits for postman to connect
        private void btn_startHttpListener_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    string ip;
                    int port;

                    if (TryGetValidIPAndPort(out ip, out port))
                    {
                        StartHttpServer().Wait();
                        break; // Exit the loop and start the server
                    }

                }
            });
        } //main ends

        public async Task StartHttpServer()
        {
            Invoke(new Action(async () =>
            {
                btn_startHttpListener.Enabled = true;

                string IpPortStrin = txtIp.Text + ":" + txtPort.Text;
                url = "http://" + IpPortStrin + "/";
                listener = new HttpListener();
                listener.Prefixes.Add(url);
                Thread.Sleep(50);
                while (true)
                {
                    try
                    {
                        try
                        {
                            Invoke(new Action(() => listener.Start()));
                            Invoke(new Action(() => label6.Text = "Server Started and waiting for client's Request!\n"));
                            btn_startHttpListener.Enabled = false;
                        }
                        catch (Exception e5)
                        {
                            MessageBox.Show(e5.Message);
                        }

                        Invoke(new Action(() => label6.Text = "Server connected to HTTP Client"));

                        context = await listener.GetContextAsync(); // retrieves the next incoming request
                        request = context.Request;
                        response = context.Response;

                        executefix = true;
                        executexml = true;
                        executejson = true;
                        executeiso = true;
                        executedelimited = true;

                        string req = "";
                        string res = "";

                        if (MessageHandler.IsJsonContentType(request.ContentType))
                        {

                            Invoke(new Action(() => MessageHandler.postmantoserverjson(request, response, ref req, ref res)));
                            Invoke(new Action(() => richTextBox1.Text = req));
                            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                            string[] respfiles = Directory.GetFiles(filepath, "*.res.txt");
                            string resp = "";
                            Invoke(new Action(() => resp = dropdownselection(respfiles, req, null, EventArgs.Empty)));
                            string respcont = File.ReadAllText(resp);
                            res = respcont;

                            _ = Invoke(new Action(() => richTextBox2.Text = res));
                            Invoke(new Action(() => checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty)));
                        }
                        else if (MessageHandler.IsXmlContentType(request.ContentType))
                        {
                            Invoke(new Action(() => MessageHandler.postmantoserverxml(request, response, ref req, ref res)));
                            Invoke(new Action(() => richTextBox1.Text = req));
                            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                            string[] respfiles = Directory.GetFiles(filepath, "*.res.txt");
                            string resp = "";
                            Invoke(new Action(() => resp = dropdownselection(respfiles, req, null, EventArgs.Empty)));
                            string respcont = File.ReadAllText(resp);
                            res = respcont;
                            _ = Invoke(new Action(() => richTextBox2.Text = res));
                            Invoke(new Action(() => checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty)));

                        }
                        else
                        {
                            // Unsupported Request
                            _ = Invoke(new Action(() => richTextBox2.Text = "Error the request is neither json nor xml"));
                            continue;
                        }


                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions here if needed
                        Invoke(new Action(() => label6.Text = "Error Connecting! " + ex.Message));
                    }
                }
                /*  finally
                  {
                       // Clean up resources when the server stops (if needed)
                       Invoke(new Action(() => label6.Text = "Connection is closed!"));
                      response.Close();
                      listener.Close();
                  }*/
                Invoke(new Action(() => label6.Text = ""));
            }));
        }


        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            //if request is in json or xml, send request to response button will be enabled
            if (MessageHandler.IsJson(richTextBox1.Text) || MessageHandler.IsXml(richTextBox1.Text))
            {
                button2.Show();
            }
            else
            {
                button2.Hide();
            }


        }

        //Checks response text and enables only that specific protocol for format conversion in which response is given
        private void protocolSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            protocolSelectionToolStripMenuItem.Checked = true;
            protocolSelectionToolStripMenuItem.Enabled = true;
            xmlToolStripMenuItem1.Enabled = false;
            iSO8583ToolStripMenuItem.Enabled = false;
            fixLengthToolStripMenuItem.Enabled = false;
            delimitedToolStripMenuItem.Enabled = false;
            jsonToolStripMenuItem.Enabled = false;
            if (MessageHandler.IsJson(richTextBox2.Text))
            {
                jsonToolStripMenuItem.Enabled = true;
            }
            else if (MessageHandler.IsXml(richTextBox2.Text))
            {
                xmlToolStripMenuItem1.Enabled = true;
            }
            else if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                delimitedToolStripMenuItem.Enabled = true;
            }
            else if (MessageHandler.IsFixedLengthData(richTextBox1.Text, richTextBox2.Text))
            {
                fixLengthToolStripMenuItem.Enabled = true;
            }
            else if (ISO.IsISO8583Format(richTextBox2.Text))
            {
                iSO8583ToolStripMenuItem.Enabled = true;
            }
            else
            {
                MessageBox.Show("Response not in either of the formats (i.e: JSON,XML,FixedLength,Delimited,ISO8583)");
            }
        }

        //Shows current response file 
        private void responseCheckboxList_SelectedIndexChanged(object sender, EventArgs e) //dropdownlist for response
        {

        }

        private static string getstringinreqformatiso(string respath)
        {
            BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();
            string[] DE = new string[130];
            DE = ISO.ReadData(respath);
            string msgtype = DE[0];
            string arr = string.Join("", DE);
            return arr;
        }

        //Find relevant response for request given, traverse whole C drive for this purpose
        private string dropdownselection(string[] responseFiles, string req, object sender, EventArgs e)
        {

            string retpath = "";
            Selectall.Text = "Select All";
            try
            {
                foreach (string filePath in responseFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                    string responseFilePath = Path.Combine(filepath, fileName + ".txt");
                    string responseContent = File.ReadAllText(responseFilePath);


                    if (MessageHandler.IsValidResponse(req, responseContent, responseFilePath))
                    {

                        if (!ISO.IsISO8583Format(req))
                        {
                            ResponseCheckboxList.Items.Add(fileName);
                            string respcont = File.ReadAllText(filePath);
                            retpath = filePath;
                            if (client == null)
                            {
                                richTextBox2.Text = respcont;
                            }
                            fixedlengthtemp = richTextBox2.Text;
                            checkedListBox1.Items.Clear();
                            checkedListBox1_SelectedIndexChanged(sender, e);
                            break;
                        }
                        else
                        {
                            ResponseCheckboxList.Items.Add(fileName);
                            if (client == null)
                            {
                                richTextBox2.Text = getstringinreqformatiso(responseFilePath);
                            }

                            checkedListBox1.Items.Clear();
                            checkedListBox1_SelectedIndexChanged(sender, e);
                            break;
                        }

                    }
                    else
                    {
                        // richTextBox2.Text = "Error in finding either request or response file";
                        continue;
                    }



                }
            }
            catch (Exception e1)
            {
                if (client == null)
                {
                    richTextBox2.Text = e1.Message;
                }
            }
            return retpath;
        }

        //Select request file from request dropdown option
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) //request dropdown
        {
            parsereq.Hide();

            richTextBox1.Clear();
            checkedListBox1.Items.Clear();
            UpdatedResponsebtn.Show();
            string selectedRequest = requestDropdown.SelectedItem.ToString();
            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
            selectedRequestFilePath = Path.Combine(filepath, selectedRequest + ".txt");
            string requestContent = File.ReadAllText(selectedRequestFilePath);
            richTextBox1.Text = requestContent;
            if (MessageHandler.IsJson(richTextBox1.Text))
            {
                button2.Show();
            }
            else if (MessageHandler.IsXml(richTextBox1.Text))
            {
                button2.Show();
            }
            else
            {
                button2.Hide();
            }
            checkedListBox2.Items.Clear();
            fixedlengthtempreq = richTextBox1.Text;

            if (!ISO.IsISO8583Format(requestContent))
            {
                executexml = true;
                executejson = true;
                executeiso = true;
                executefix = true;
                executedelimited = true;

                executefixreq = true;
                executexmlreq = true;
                executejsonreq = true;
                executeisoreq = true;
                executedelimitedreq = true;
                richTextBox1.Text = requestContent;
                checkedListBox2.Items.Clear();
                fixedlengthtempreq = richTextBox1.Text;
                checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty);
            }
            else
            {
                executexml = true;
                executejson = true;
                executeiso = true;
                executefix = true;
                executedelimited = true;

                executefixreq = true;
                executexmlreq = true;
                executejsonreq = true;
                executeisoreq = true;
                executedelimitedreq = true;
                richTextBox1.Text = requestContent;
                checkedListBox2.Items.Clear();
                fixedlengthtempreq = richTextBox1.Text;
                checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty);
            }

            richTextBox2.Clear();
            responseContent = null;
            ResponseCheckboxList.Items.Clear();

            string file = ConfigurationManager.AppSettings["FilePath"].ToString();
            string[] responseFiles = Directory.GetFiles(file, "*.res.txt");
            dropdownselection(responseFiles, requestContent, sender, e);
        }

        private void UpdateResponseFields(string responseContent)
        {
            // Parse the response content and extract the fields
            dynamic responseJson = JObject.Parse(responseContent);
            List<string> fields = GetResponseFields(responseJson);

            // Create and add checkboxes for each field in the responseFieldsPanel
            int y = 0;
            foreach (string field in fields)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Text = field;
                checkBox.Location = new Point(10, y);
                checkBox.AutoSize = true;
                y += checkBox.Height + 5;
            }
        }

        private List<string> GetResponseFields(dynamic responseJson)
        {
            List<string> fields = new List<string>();

            // Implement your logic to extract the response fields from the JSON object
            // This is just an example, replace it with your actual logic

            foreach (JProperty property in responseJson.Properties())
            {
                fields.Add(property.Name);
            }

            return fields;
        }

        private string responseFieldsCheckboxList_SelectedIndexChanged(object sender, EventArgs e)
        {

            string selectedResponse = ResponseCheckboxList.SelectedValue.ToString();
            string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
            selectedResponseFilePath = Path.Combine(filepath, selectedResponse + ".txt");
            string responseContent = File.ReadAllText(selectedResponseFilePath);
            richTextBox2.Text = responseContent;
            UpdateResponseFields(responseContent);
            return selectedResponseFilePath;
        }

        private void xmlToolStripMenuItem_Click(object sender, EventArgs e) //json to xml converter
        {
            executexml = true;
            string json = richTextBox2.Text;
            if (MessageHandler.IsXml(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in XML!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                checkedListBox1.Items.Clear();
                richTextBox2.Text = MessageHandler.ConvertJsonToXml(json);
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }
        private void fixedLengthToolStripMenuItem_Click(object sender, EventArgs e) //Json to Fixlength
        {
            executefix = true;
            string fix = richTextBox2.Text;
            if (MessageHandler.IsFixedLengthData(richTextBox1.Text, richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in Fixedlength!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                List<string> list = FixedLength.ConvertJsonToFixedLength(fix);
                foreach (string item in list)
                {
                    richTextBox2.Text += item;
                }
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);

            }
        }
        private void jsonToolStripMenuItem1_Click(object sender, EventArgs e) //Xml to Json
        {
            executejson = true;
            string xml = richTextBox2.Text;
            if (MessageHandler.IsJson(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in Json!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = MessageHandler.ConvertXmlToJson(xml);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }
        private void fixLengthToolStripMenuItem1_Click(object sender, EventArgs e) //Xml to Fixlength
        {
            executefix = true;
            string fix = richTextBox2.Text;
            if (MessageHandler.IsFixedLengthData(richTextBox1.Text, richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in Fixlength!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = FixedLength.ConvertXmlToFixLen(fix);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }
        private void delimitedToolStripMenuItem2_Click(object sender, EventArgs e) //Xml to Delimited
        {
            executedelimited = true;
            string deli = richTextBox2.Text;
            if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                label7.Text = "";
                label7.Text = "Response already in delimited format!";
            }
            else
            {
                label7.Text = "";
                // richTextBox2.Text = "";
                try
                {
                    richTextBox2.Text = Delimited.ConvertXmlToDelimited(deli, delimiter);
                    checkedListBox1.Items.Clear();
                    // listBox1.Items.Clear();
                    checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
                }
                catch (Exception e4)
                {
                    richTextBox2.Text = e4.Message;
                }
            }
        }
        private void jsonToolStripMenuItem2_Click(object sender, EventArgs e) //Fix length to Json
        {
            executejson = true;
            string json = richTextBox2.Text;
            if (MessageHandler.IsJson(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in JSON!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = FixedLength.ConvertFixLenToJson("\"" + json + "\"");
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);

            }
        }

        private void xmlToolStripMenuItem2_Click(object sender, EventArgs e) //Fix length to Xml
        {
            executexml = true;
            string xml = richTextBox2.Text;
            if (MessageHandler.IsXml(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in XML!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = FixedLength.ConvertFixLenToXml("\"" + xml + "\"");
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);

            }
        }
        private void xmlToolStripMenuItem3_Click(object sender, EventArgs e) //Delimited to Xml
        {
            executexml = true;
            string[] array = richTextBox2.Text.Split('|');
            // Create a new string with each element on a separate line
            if (MessageHandler.IsXml(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in XML!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = Delimited.ConvertDelimitedToXml(array, delimiter);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }

        }

        private void jsonToolStripMenuItem3_Click(object sender, EventArgs e) //Delimited to Json
        {
            executejson = true;
            string json = richTextBox2.Text;
            if (MessageHandler.IsJson(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in JSON!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = Delimited.ConvertDelimitedToJson(json.Split(delimiter), delimiter);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }

        }
        private void jsonToolStripMenuItem4_Click(object sender, EventArgs e) //ISO to Json
        {
            executejson = true;
            string[] isomsg = getitemsaftercolon(checkedListBox1);
            if (MessageHandler.IsJson(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in JSON!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = ISO.ConvertIso8583ToJson(isomsg);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }

        }
        private void xmlToolStripMenuItem4_Click(object sender, EventArgs e) //ISO to Xml
        {
            executexml = true;
            if (MessageHandler.IsXml(richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in XML!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = ISO.ConvertIso8583ToXml(getitemsaftercolon(checkedListBox1));
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        //Extracts isomessage and uses third party library to store it in DE message array. It stores MIT in last index ie:[129]   
        private string[] getDE()
        {
            BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();
            string[] DE = new string[130];
            DE = iso8583.Parse(richTextBox2.Text);
            return DE;
        }
        private string[] getDEreq()
        {
            BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();
            string[] DE = new string[130];
            try
            {
                DE = iso8583.Parse(richTextBox1.Text);
            }
            catch (Exception e10)
            {
                MessageBox.Show(e10.Message);
            }
            return DE;
        }

        private void processjsonobj(JObject jsonobj)
        {

            foreach (JProperty property in jsonobj.Properties())
            {
                string propertyName = property.Name;
                JToken propertyValue = property.Value;
                checkedListBox1.Items.Add(propertyName + ":" + propertyValue + "&");

            }
        }
        private void ProcessArray(JObject jsonarr)
        {
            JArray jsonArray = JArray.Parse(richTextBox2.Text);
            // Code to handle JSON array
            foreach (var item in jsonArray)
            {
                if (item.Type == JTokenType.Object)
                {
                    processjsonobj(item.Value<JObject>());
                }
                else
                {
                    // For simple values (e.g., integers, strings, etc.), just add them to the CheckedListBox
                    checkedListBox1.Items.Add(item.ToString());
                }
            }
        }
        //For handling json nested objects and arrays, (Not implemented on simulator)
        private void AddPropertiesToCheckedListBox(JToken token, string prefix = "")
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>())
                    {
                        try
                        {
                            AddPropertiesToCheckedListBox(property.Value, $"{prefix}{property.Name}: ");
                        }
                        catch (InvalidOperationException ioe)
                        {
                            MessageBox.Show(ioe.Message);
                        }
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (var item in token.Children())
                    {
                        AddPropertiesToCheckedListBox(item, $"{prefix}[{index}]: ");
                        index++;
                    }
                    break;

                default:
                    checkedListBox2.Items.Add($"{prefix}{token}&");
                    break;
            }
        }
        //Function to fetch data from response and parse it into fields in checklistbox
        private void populatechecklistboxresp()
        {
            if (MessageHandler.IsJson(richTextBox2.Text) && richTextBox2.Text.StartsWith("{"))
            {
                if (executejson)
                {
                    JObject json = JObject.Parse(richTextBox2.Text);
                    foreach (JProperty item in json.Properties())
                    {
                        // Access the node name and inner text
                        string name = item.Name;
                        JToken value = item.Value;
                        checkedListBox1.Items.Add(name + ":" + value + "&");
                    }
                }
                executejson = false;
            }
            else if (MessageHandler.IsXml(richTextBox2.Text))
            {
                //Working for xml response
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(richTextBox2.Text);

                // Get the root element
                XmlElement rootElement = xmlDoc.DocumentElement;

                if (executexml == true)
                {
                    foreach (XmlNode node in rootElement.ChildNodes)
                    {
                        // Access the node name and inner text
                        string nodeName = node.Name;
                        string nodeValue = node.InnerText;
                        checkedListBox1.Items.Add(nodeName + ":" + nodeValue + "&");
                    }
                    executexml = false;
                }
            }

            else if (richTextBox1.Text.Substring(0, 5) == richTextBox2.Text.Substring(0, 5) && !richTextBox2.Text.Contains(delimiter)
                && !richTextBox2.Text.Contains('{') && !richTextBox2.Text.Contains('<') && !richTextBox2.Text.Contains("MIT"))
            {
                ///richTextBox2.Text = fixedlengthtemp;
                int[] fieldLengths = new int[40];
                for (int i = 0; i < fieldLengths.Length; i++)
                {
                    fieldLengths[i] = 5;
                }

                int ind = 0;

                string[] field = new string[fieldLengths.Length];
                if (executefix)
                {
                    for (int i = 0; i < fieldLengths.Length; i++)
                    {
                        if (ind >= 0 && ind < richTextBox2.Text.Length && fieldLengths[i] > 0 && ind < richTextBox2.Text.Length)
                        {
                            string substring = "";
                            if ((ind + fieldLengths[i]) <= richTextBox2.Text.Length)
                            {
                                substring = richTextBox2.Text.Substring(ind, fieldLengths[i]);
                            }
                            else
                            {
                                substring = richTextBox2.Text.Substring(ind);
                            }

                            string paddedSubstring = substring.PadRight(fieldLengths[i], ' ');
                            checkedListBox1.Items.Add(paddedSubstring + '&');
                            checkedListBox1.SetItemChecked(0, true);
                            ind += fieldLengths[i];
                        }
                        else
                        {
                            break;
                        }

                    }
                    executefix = false;
                }
            }
            else if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                string[] fields = richTextBox2.Text.Split(delimiter);
                if (executedelimited)
                {
                    foreach (string field in fields)
                    {
                        checkedListBox1.Items.Add(field + "&");
                    }
                    executedelimited = false;
                }
            }

            else if (ISO.IsISO8583Format(richTextBox2.Text))
            {
                if (executeiso)
                {
                    if (ISO.matchproccode(richTextBox1.Text) == ISO.matchproccode(richTextBox2.Text))
                    {
                        string[] DE = getDE();

                        for (int i = 0; i < DE.Length; i++)
                        {
                            if (i == 129)
                            {
                                Console.WriteLine("MIT: " + DE[i]);
                            }
                            if (DE[i] != null && i != 129)
                            {
                                checkedListBox1.Items.Add("Bit- " + i + " , " + ISO.gethashvalue(i) + ": " + DE[i] + "&");
                            }
                        }
                    }
                    executeiso = false;
                }
            }
        }
        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            populatechecklistboxresp();
        }

        //Convert request to xml
        public static string ConvertToXml(string runtimeText)
        {
            StringWriter stringWriter = new StringWriter();

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings))
            {
                xmlWriter.WriteStartElement("Root");

                string[] keyValuePairs = runtimeText.Split('&');
                foreach (string keyValuePair in keyValuePairs)
                {
                    string[] parts = keyValuePair.Split(':');
                    if (parts.Length == 2)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        xmlWriter.WriteStartElement(key);
                        xmlWriter.WriteValue(value);
                        xmlWriter.WriteEndElement();
                    }
                }

                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
            }
            return stringWriter.ToString();
        }

        //Converts fields to fixlength format and populate request
        private void printfix(string input)
        {
            string fix = input;
            int[] fieldLengths = new int[40];
            for (int i = 0; i < fieldLengths.Length; i++)
            {
                fieldLengths[i] = 5;
            }
            int ind = 0;
            StringBuilder resultBuilder = new StringBuilder();

            // Iterate over the field lengths and selected checkboxes
            for (int i = 0; i < fieldLengths.Length && i < checkedListBox2.Items.Count; i++)
            {
                // Check if the checkbox is selected
                if (checkedListBox2.GetItemChecked(i))
                {
                    // Check if there is enough input data for the field length
                    if (fix.Length >= fieldLengths[i])
                    {
                        // Append the substring to the result
                        resultBuilder.Append(fix.Substring(ind, fieldLengths[i]));
                        fix = fix.Remove(ind, fieldLengths[i]);
                    }
                    else
                    {
                        // If the input is shorter than the field length, pad with white spaces
                        resultBuilder.Append(fix.PadRight(fieldLengths[i], ' '));
                        fix = string.Empty;
                        ind += fieldLengths[i];
                    }
                }
                else
                {
                    // If the checkbox is not selected, add white spaces
                    resultBuilder.Append("".PadRight(fieldLengths[i], ' '));
                    ind += fieldLengths[i];
                }
            }

            richTextBox1.Text = resultBuilder.ToString();

        }
        //Converts fields to fixlength format and populate response
        private void printfixres(string input)
        {
            string fix = input;
            int[] fieldLengths = new int[40];
            for (int i = 0; i < fieldLengths.Length; i++)
            {
                fieldLengths[i] = 5;
            }
            int ind = 0;
            StringBuilder resultBuilder = new StringBuilder();

            // Iterate over the field lengths and selected checkboxes
            for (int i = 0; i < fieldLengths.Length && i < checkedListBox1.Items.Count; i++)
            {
                // Check if the checkbox is selected
                if (checkedListBox1.GetItemChecked(i))
                {
                    // Check if there is enough input data for the field length
                    if (fix.Length >= fieldLengths[i])
                    {
                        // Append the substring to the result
                        resultBuilder.Append(fix.Substring(ind, fieldLengths[i]));
                        fix = fix.Remove(ind, fieldLengths[i]);
                    }
                    else
                    {
                        // If the input is shorter than the field length, pad with white spaces
                        resultBuilder.Append(fix.PadRight(fieldLengths[i], ' '));
                        fix = string.Empty;
                        ind += fieldLengths[i];
                    }
                }
                else
                {
                    // If the checkbox is not selected, add white spaces
                    resultBuilder.Append("".PadRight(fieldLengths[i], ' '));
                    ind += fieldLengths[i];
                }
            }
            richTextBox2.Text = resultBuilder.ToString();
        }

        //get response text that is present on right side of colon, in ISO response file
        private string[] getitemsaftercolon(CheckedListBox checkedListBox1)
        {
            List<string> dataArray = new List<string>();
            foreach (var checkedItem in checkedListBox1.CheckedItems)
            {
                string itemText = checkedListBox1.GetItemText(checkedItem);
                string[] splitArray = itemText.Split(':');
                if (splitArray.Length > 1)
                {
                    string data = splitArray[1].Trim();
                    dataArray.Add(data);
                }
            }
            string[] dataArrayResult = dataArray.Select(checkeditem => checkeditem.Replace("&", "")).ToArray();
            return dataArrayResult;
        }
        private string[] getitemsaftercolonreq(CheckedListBox checkedListBox2)
        {
            List<string> dataArray = new List<string>();
            foreach (var checkedItem in checkedListBox2.CheckedItems)
            {
                string itemText = checkedListBox2.GetItemText(checkedItem);
                string[] splitArray = itemText.Split(':');
                if (splitArray.Length > 1)
                {
                    string data = splitArray[1].Trim();
                    dataArray.Add(data);
                }
            }
            string[] dataArrayResult = dataArray.Select(checkeditem => checkeditem.Replace("&", "")).ToArray();
            return dataArrayResult;
        }
        private JObject ConvertSelectedItemToJson(string input)
        {
            // Remove the trailing comma from the input string
            input = input.TrimEnd(',');

            // Split the input string into key-value pairs
            string[] keyValuePairs = input.Split('&');

            // Create a new JObject to store the JSON object
            JObject jsonObject = new JObject();

            // Iterate through the key-value pairs
            foreach (string pair in keyValuePairs)
            {
                // Split each pair into key and value
                string[] parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim('&');
                    JToken value = parts[1].Trim('&');
                    jsonObject.Add(key, value);
                    // Add key-value pair to the JSON object
                }
            }

            return jsonObject;
        }

        //Send selected response fields to client
        private async void selectrespbtt()
        {
            Invoke(new Action(() =>
            {
                if (listener == null)
                {
                    if (checkedListBox1.CheckedItems.Count <= 0)
                    {
                        MessageBox.Show("Please select any Field!");
                        return;
                    }
                }

                string str = "";
                string text = "";

                foreach (string item in checkedListBox1.CheckedItems)
                {
                    str += item;
                }
                Invoke(new Action(() => richTextBox2.Show()));
                Invoke(new Action(() => label5.Show()));

                if (MessageHandler.IsXml(richTextBox2.Text))
                {
                    text = str.TrimEnd('&');
                    string xml = ConvertToXml(text);
                    Invoke(new Action(() => richTextBox2.Text = xml));
                }
                else if (MessageHandler.IsJson(richTextBox2.Text) && richTextBox2.Text.StartsWith("{"))
                {
                    text = str.TrimEnd('&');

                    // Convert the selected item to JSON format
                    JObject selectedFieldObject = ConvertSelectedItemToJson(text);

                    // Display the JSON result in the RichTextBox
                    Invoke(new Action(() => richTextBox2.Text = selectedFieldObject.ToString()));
                }


                if (listener == null)
                {
                    if (richTextBox1.Text.Substring(0, 5) == richTextBox2.Text.Substring(0, 5) && !richTextBox2.Text.Contains(delimiter)
                   && !richTextBox2.Text.Contains('{') && !richTextBox2.Text.Contains('<'))
                    {
                        string fx = richTextBox2.Text.TrimEnd('&');
                        Invoke(new Action(() => printfixres(fx)));
                    }

                    else if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
                    {

                        richTextBox2.Text = "";
                        string delim = "";
                        string[] array = richTextBox2.Text.Split(delimiter);
                        foreach (string items in checkedListBox1.CheckedItems)
                        {
                            string field = items.Replace('&', delimiter);
                            delim += field.ToString();
                            Invoke(new Action(() => richTextBox2.Text = delim.TrimEnd(delimiter)));
                        }
                    }

                    else if (ISO.IsISO8583Format(richTextBox2.Text))
                    {
                        string[] array = getDE();
                        string MTI = array[129];
                        Invoke(new Action(() => richTextBox2.Text = MTI + string.Join("", getitemsaftercolon(checkedListBox1)).ToString()));
                    }

                }

                if (socketforclients != null)
                {
                    NetworkStream ns = new NetworkStream(socketforclients);
                    sw = new StreamWriter(ns, Encoding.UTF8, 4096, true);
                    string message = richTextBox2.Text;
                    // Convert the message to bytes
                    byte[] messageData = Encoding.UTF8.GetBytes(message);
                    string sendtoclient = (Encoding.UTF8.GetString(messageData));

                    sw.WriteLine(sendtoclient);
                    sw.Flush();
                    sw.Close();
                }

                if (listener != null)
                {
                    string responseText = richTextBox2.Text; // The text you want to send back as the response
                    byte[] buffer = Encoding.UTF8.GetBytes(responseText); // Convert the text to bytes using UTF-8 encoding

                    Stream responseStream = response.OutputStream;

                    response.ContentLength64 = buffer.Length; // Set the content length in the response

                    responseStream.Write(buffer, 0, buffer.Length); // Write the response content to the output stream

                }

                button1.Enabled = true;
                btn_startHttpListener.Enabled = true;
            }));
        }

        //Send selected request fields to server
        private async void selectreqbtt()
        {
            if (checkedListBox2.CheckedItems.Count <= 0)
            {
                MessageBox.Show("Please select any Field!");
                return;
            }

            await Task.Run(async () =>
            {
                string str = "";
                string text = "";
                foreach (string item in checkedListBox2.CheckedItems)
                {
                    str += item;
                }
                richTextBox1.Invoke(new Action(async () =>
                {
                    richTextBox1.Show();
                    label5.Show();
                    if (MessageHandler.IsXml(richTextBox1.Text))
                    {
                        text = str.TrimEnd('&');
                        string xml = ConvertToXml(text);
                        richTextBox1.Text = xml;
                    }

                    else if (MessageHandler.IsJson(richTextBox1.Text) && richTextBox1.Text.StartsWith("{"))
                    {
                        text = str.TrimEnd('&');
                        // Convert the selected item to JSON format
                        JObject selectedFieldObject = ConvertSelectedItemToJson(text);

                        // Display the JSON result in the RichTextBox
                        richTextBox1.Text = selectedFieldObject.ToString();
                    }
                    else if (richTextBox1.Text.Substring(0, 4) != "0200" && !richTextBox1.Text.Contains(delimiter)
                        && !richTextBox1.Text.Contains('{') && !richTextBox1.Text.Contains('<'))
                    {
                        string fx = richTextBox1.Text.TrimEnd('&');
                        printfix(fx);
                    }

                    else if (MessageHandler.IsDelimitedMessage(richTextBox1.Text, delimiter))
                    {
                        richTextBox1.Text = "";
                        string delim = "";
                        string[] array = richTextBox1.Text.Split(delimiter);
                        foreach (string items in checkedListBox2.CheckedItems)
                        {
                            string field = items.Replace('&', delimiter);
                            delim += field.ToString();
                            richTextBox1.Text = delim.TrimEnd(delimiter);
                        }
                    }

                    else if (ISO.IsISO8583Format(richTextBox1.Text))
                    {
                        string[] array = getDEreq();
                        string MTI = array[129];
                        richTextBox1.Text = MTI + string.Join("", getitemsaftercolonreq(checkedListBox2)).ToString();
                    }
                    if (client != null)
                    {
                        try
                        {
                            // Convert the request string to bytes
                            byte[] requestData = System.Text.Encoding.UTF8.GetBytes(richTextBox1.Text);

                            // Send the request to the server
                            ns.Write(requestData, 0, requestData.Length);
                            ns.Flush();
                            int bytesRead = 0;
                            byte[] buffer = new byte[4096];
                            while (true)
                            {
                                Thread.Sleep(10);
                                bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    Invoke(new Action(() => richTextBox2.Text = response));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            richTextBox2.Text = "Error: " + ex.Message;
                        }
                    }

                    if (socketforclients != null) //for server side
                    {
                        NetworkStream ns = new NetworkStream(socketforclients);
                        StreamWriter sw = new StreamWriter(ns, Encoding.UTF8, 4096, true);
                        string message = richTextBox1.Text;
                        // Convert the message to bytes
                        byte[] messageData = Encoding.ASCII.GetBytes(message);
                        string sendtoclient = (Encoding.ASCII.GetString(messageData));
                        sw.WriteLine(sendtoclient);
                        sw.Close();
                    }

                    if (listener != null) //for postman connection
                    {
                        string responseText = richTextBox1.Text; // The text you want to send back as the response
                        byte[] buffer = Encoding.UTF8.GetBytes(responseText); // Convert the text to bytes using UTF-8 encoding
                        response.ContentLength64 = buffer.Length; // Set the content length in the response
                        using (Stream responseStream = response.OutputStream)
                        {
                            responseStream.Write(buffer, 0, buffer.Length); // Write the response content to the output stream
                        }
                    }

                    button1.Enabled = true;
                    btn_startHttpListener.Enabled = true;
                }));
            });
        }

        private async void btnProcessResponse_Click(object sender, EventArgs e) //Select Response
        {
            await Task.Run(() =>
            {
                selectrespbtt();
            });

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e) //Temporary Change to file
        {

        }

        private void button5_Click(object sender, EventArgs e) //save response to file
        {
            string filePath = "";
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // Set the title of the dialog
                saveFileDialog.Title = "Save As";

                // Set the default file name and extension
                saveFileDialog.FileName = "document.txt";
                saveFileDialog.DefaultExt = "txt";

                // Set the initial directory (optional)
                string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                saveFileDialog.InitialDirectory = filePath;

                // Set the filter for the file types to be displayed
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

                // Show the "Save As" dialog and check if the user clicked the OK button
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // The user selected a file path, and you can now save your content to this path
                    filePath = saveFileDialog.FileName;
                    // Add your save logic here
                    // For example: File.WriteAllText(filePath, content);
                }
                else
                {
                    filePath = saveFileDialog.FileName;
                }
            }
            File.WriteAllText(filePath, richTextBox2.Text);
        }

        //Select all fields in response
        void CheckAllItemres(CheckedListBox checkedListBox)
        {
            if (checkedListBox2.Items.Count <= 0)
            {
                MessageBox.Show("No field present to select!");
                return;
            }
            if (mouseclick % 2 != 0)
            {
                mouseclick++;
                Selectall.Text = "Unselect All";
                Selectall.ForeColor = System.Drawing.Color.White;
                Selectall.BackColor = System.Drawing.Color.Black;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, true);
                }
            }

            else
            {
                mouseclick++;
                Selectall.Text = "Select All";
                Selectall.ForeColor = System.Drawing.Color.Black;
                Selectall.BackColor = System.Drawing.Color.White;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, false);
                }
            }
        }

        //Select all fields in request
        void CheckAllItemsreq(CheckedListBox checkedListBox)
        {
            if (checkedListBox2.Items.Count <= 0)
            {
                MessageBox.Show("No field present to select!");
                return;
            }
            if (mouseclick % 2 != 0)
            {
                mouseclick++;
                Selectallreq.Text = "Unselect All";
                Selectallreq.ForeColor = System.Drawing.Color.White;
                Selectallreq.BackColor = System.Drawing.Color.Black;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, true);
                }
            }
            else
            {
                mouseclick++;
                Selectallreq.Text = "Select All";
                Selectallreq.ForeColor = System.Drawing.Color.Black;
                Selectallreq.BackColor = System.Drawing.Color.White;
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    checkedListBox.SetItemChecked(i, false);
                }
            }
        }
        private void Selectall_Click(object sender, EventArgs e)
        {

            CheckAllItemres(checkedListBox1);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e) //open file
        {

        }

        private void delimitedToolStripMenuItem1_Click(object sender, EventArgs e)//json to delimited
        {
            executedelimited = true;
            string deli = richTextBox2.Text;
            if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                label7.Text = "";
                label7.Text = "Response already in Delimited!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = Delimited.ConvertJsonToDelimitedMessage(deli);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void delimitedToolStripMenuItem3_Click(object sender, EventArgs e) //ISO to delimited
        {
            executedelimited = true;
            string delim = richTextBox2.Text;
            if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                label7.Text = "";
                label7.Text = "Response already in Delimited!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = ISO.ConvertIsoToDelimited(delim, delimiter);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void xmlToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void iSO8583ToolStripMenuItem1_Click(object sender, EventArgs e) //json to iso
        {
            try
            {
                executeiso = true;
                string iso = richTextBox2.Text;
                if (ISO.IsISO8583Format(richTextBox2.Text))
                {
                    label7.Text = "";
                    label7.Text = "Response already in ISO8583!";
                }
                else
                {
                    label7.Text = "";
                    richTextBox2.Text = "";
                    richTextBox2.Text = MessageHandler.convertjsontoiso(iso);
                    checkedListBox1.Items.Clear();
                    checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
                }
            }
            catch (Exception e5)
            {
                MessageBox.Show(e5.Message);
            }

        }

        private void iSO8583ToolStripMenuItem2_Click(object sender, EventArgs e) //xml to iso
        {
            try
            {
                executeiso = true;
                string iso = richTextBox2.Text;
                if (ISO.IsISO8583Format(richTextBox2.Text))
                {
                    label7.Text = "";
                    label7.Text = "Response already in ISO!";
                }
                else
                {
                    label7.Text = "";
                    richTextBox2.Text = "";
                    richTextBox2.Text = MessageHandler.convertxmltoiso(iso);
                    checkedListBox1.Items.Clear();
                    checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);

                }
            }
            catch (Exception e5)
            {
                MessageBox.Show(e5.Message);
            }
        }

        private void fixLengthToolStripMenuItem2_Click(object sender, EventArgs e) //ISO to fixedlength
        {
            executefix = true;
            string fixedlen = richTextBox2.Text;
            if (MessageHandler.IsFixedLengthData(richTextBox1.Text, richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in Fixedlength!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = ISO.ConvertIsoToFixedLength(fixedlen);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void fixLengthToolStripMenuItem3_Click(object sender, EventArgs e) //Delimited to Fixedlength
        {
            executefix = true;
            string fixedlen = richTextBox2.Text;
            if (MessageHandler.IsFixedLengthData(richTextBox1.Text, richTextBox2.Text))
            {
                label7.Text = "";
                label7.Text = "Response already in Fixedlength!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = Delimited.ConvertDelimitedToFixLen(fixedlen, delimiter);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void iSO8583ToolStripMenuItem3_Click(object sender, EventArgs e) //Delimited to ISO
        {
            try
            {
                executeiso = true;
                string iso = richTextBox2.Text;
                if (ISO.IsISO8583Format(richTextBox2.Text))
                {
                    label7.Text = "";
                    label7.Text = "Response already in ISO8583!";
                }
                else
                {
                    label7.Text = "";
                    richTextBox2.Text = "";
                    richTextBox2.Text = Delimited.ConvertDelimitedToIso(iso, delimiter);
                    checkedListBox1.Items.Clear();
                    checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
                }
            }
            catch (Exception e5)
            {
                MessageBox.Show(e5.Message);
            }
        }

        private void delimitedToolStripMenuItem4_Click(object sender, EventArgs e) //FixedLength to Delimited
        {
            executedelimited = true;
            string deli = richTextBox2.Text;
            if (MessageHandler.IsDelimitedMessage(richTextBox2.Text, delimiter))
            {
                label7.Text = "";
                label7.Text = "Response already in Delimited!";
            }
            else
            {
                label7.Text = "";
                richTextBox2.Text = "";
                richTextBox2.Text = FixedLength.ConvertFixLenToDelimited(deli, delimiter);
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripComboBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_Click_2(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            // Check if there is a selected item in the combobox
            if (FileOpen.SelectedItem != null)
            {
                string selectedItem = FileOpen.SelectedItem.ToString();
                string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                string filePath = Path.Combine(filepath + selectedItem + ".txt");
                try
                {
                    // Check if the file exists before attempting to open it
                    if (File.Exists(filePath))
                    {
                        Process.Start(filePath);
                    }
                    else
                    {
                        // Handle the case when the file does not exist
                        MessageBox.Show("The selected file does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur when attempting to open the file
                    MessageBox.Show("An error occurred while opening the file: " + ex.Message);
                }
            }
            else
            {
                // Handle the case when no item is selected in the combobox
                MessageBox.Show("Please select a file from the dropdown list.");
            }
        }

        private void back_Click(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                if (c is RichTextBox || c is Label || c is Button || c is TextBox || c is ListBox || c is CheckBox)
                {
                    editfields.Visible = true;
                    c.Visible = true;
                }
            }
            richTextBox3.Visible = false;
            back.Visible = false;
            FileOpen.Enabled = true;
            protocolSelectionToolStripMenuItem.Enabled = true;
            requestDropdown.Enabled = true;
            selectTypeToolStripMenuItem.Enabled = true;
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void txtIp_MouseHover(object sender, EventArgs e) //ipmousehover
        {
            iplabel.Text = "Enter IP (Format: XXX.XXX.XXX.XXX)";
        }

        private void txtIp_MouseLeave(object sender, EventArgs e) //ipmouseleave
        {
            iplabel.Text = "";
        }

        private void txtPort_MouseHover(object sender, EventArgs e)
        {
            portlabel.Text = "Enter Port (Format: XXXXX)";
        }

        private void txtPort_MouseLeave(object sender, EventArgs e)
        {
            portlabel.Text = "";
        }

        private void protocolSelectionToolStripMenuItem_MouseHover_1(object sender, EventArgs e) //protocollabel hover
        {
            protocolSelectionToolStripMenuItem.Checked = true;
            protocollabel.Text = "This option changes format in response into desired format";
        }

        private void protocolSelectionToolStripMenuItem_MouseLeave(object sender, EventArgs e)  //protocollabel leave
        {
            protocolSelectionToolStripMenuItem.Checked = false;
            protocollabel.Text = "";
        }

        private void aBoutUsToolStripMenuItem_MouseHover(object sender, EventArgs e) //aboutlabel hover
        {
            Aboutlabel.Text = "Displays simulator creators info";
        }

        private void aBoutUsToolStripMenuItem_MouseLeave(object sender, EventArgs e) //aboutlabel leave
        {
            Aboutlabel.Text = "";
        }

        private void requestDropdown_MouseHover(object sender, EventArgs e) //reqdroplabel hover
        {
            reqdroplabel.Text = "Displays all req files in C-drive";
        }

        private void requestDropdown_MouseLeave(object sender, EventArgs e) //reqdroplabel leave
        {
            reqdroplabel.Text = "";
        }

        private void Selectall_MouseHover(object sender, EventArgs e) //selectalllabel hover
        {
            selectalllabel.Text = "Select all response fields";
        }

        private void Selectall_MouseLeave(object sender, EventArgs e) //selectalllabel leave
        {
            selectalllabel.Text = "";
        }

        private void UpdatedResponsebtn_MouseHover(object sender, EventArgs e) //selectresponselabel hover
        {
            selectreslabel.Text = "Send Response Fields back to Client";
        }

        private void UpdatedResponsebtn_MouseLeave(object sender, EventArgs e) //selectresponselabel leave
        {
            selectreslabel.Text = "";
        }

        private void button1_MouseHover(object sender, EventArgs e) //startlistenlabel hover
        {
            listenlabel.Text = "Listen to Client Request";
        }

        private void button1_MouseLeave(object sender, EventArgs e) //startlistenlabel leave
        {
            listenlabel.Text = "";
        }

        private void btn_startHttpListener_MouseHover(object sender, EventArgs e) //httplabel hover
        {
            httplabel.Text = "Listen to Postman Request";
        }

        private void btn_startHttpListener_MouseLeave(object sender, EventArgs e) //httplabel leave
        {
            httplabel.Text = "";
        }

        private void FileOpen_MouseHover(object sender, EventArgs e)
        {
            fileselectlabel.Text = "Opens selected file";

        }

        private void FileOpen_MouseLeave(object sender, EventArgs e)
        {
            fileselectlabel.Text = "";

        }

        private void SetButtonLocationToBottomRight()
        {
            // Adjust the margin from the right and bottom edges of the screen
            int marginFromRight = 20;  // Adjust this value as needed
            int marginFromBottom = 20; // Adjust this value as needed

            // Calculate the position for the button
            int buttonX = ClientSize.Width - back.Width - marginFromRight;
            int buttonY = ClientSize.Height - back.Height - marginFromBottom;

            // Set the button's location
            back.Location = new System.Drawing.Point(buttonX, buttonY);
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            SetButtonLocationToBottomRight(); // Call the method when the form is resized
        }

        //delete selected fields
        private void button6_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please Select any item!");
                return;
            }
            DialogResult choose = MessageBox.Show("Do you want to delete selected fields?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choose == DialogResult.No)
            {
                return;
            }
            else if (choose == DialogResult.Yes)
            {
                // Use a for loop to safely remove items while iterating in reverse
                for (int i = checkedListBox1.CheckedItems.Count - 1; i >= 0; i--)
                {
                    string item = checkedListBox1.CheckedItems[i].ToString();
                    checkedListBox1.Items.Remove(item);
                }
            }
        }

        //send custom response in xml to client
        private void xmlsendmsg(string[] fieldNamesArray, string[] fieldValuesArray)
        {
            if (socketforclients != null)
            {
                NetworkStream ns = new NetworkStream(socketforclients);
                StreamWriter sw = new StreamWriter(ns, new UTF8Encoding(false)); // Use UTF-8 encoding without the BOM                                                    
                string xm = MessageHandler.ConvertToXML(fieldNamesArray, fieldValuesArray);
                if (MessageHandler.IsXml(richTextBox2.Text))
                {
                    string data = addmanual(xm);
                    XDocument doc = XDocument.Parse(data);
                    data = doc.ToString();
                    richTextBox2.Text = data;
                    sw.WriteLine(data);
                    sw.Flush();
                    sw.Close();
                }
                else if (MessageHandler.IsJson(richTextBox2.Text))
                {
                    MessageBox.Show("Please select valid message format!");
                }
                else
                {
                    XDocument doc = XDocument.Parse(xm);
                    richTextBox2.Text = doc.ToString();
                    sw.WriteLine(richTextBox2.Text);
                    sw.Flush();
                    sw.Close();
                }
            }

            else if (listener != null)
            {
                string xml = MessageHandler.ConvertToXML(fieldNamesArray, fieldValuesArray);
                byte[] buffer = Encoding.UTF8.GetBytes(xml); // Convert the text to bytes using UTF-8 encoding
                response.ContentLength64 = buffer.Length; // Set the content length in the response
                using (Stream responseStream = response.OutputStream)
                {
                    responseStream.Write(buffer, 0, buffer.Length); // Write the response content to the output stream
                }
            }
            else
            {
                MessageBox.Show("Server is not connected to server to either socket or postman!");
            }
            sendxml.Hide();
            sendjson.Hide();
        }

        //send custom response in xjsonml to client
        private void jsonsendmsg(string[] fieldNamesArray, string[] fieldValuesArray)
        {
            if (socketforclients != null)
            {
                NetworkStream ns = new NetworkStream(socketforclients);
                StreamWriter sw = new StreamWriter(ns, new UTF8Encoding(false)); // Use UTF-8 encoding without the BOM                                                        //string message = "Field value: " + fv + " Field length: " + fl + " Field name: " + fn;
                string js = MessageHandler.ConvertToJSON(fieldNamesArray, fieldValuesArray);
                if (MessageHandler.IsJson(richTextBox2.Text))
                {
                    string data = addmanual(js);
                    sw.WriteLine(data);
                    sw.Flush();
                    sw.Close();
                }
                else if (MessageHandler.IsXml(richTextBox2.Text))
                {
                    MessageBox.Show("Please select valid message format!");
                }
                else
                {
                    richTextBox2.Text = js;
                    sw.WriteLine(richTextBox2.Text);
                    sw.Flush();
                    sw.Close();
                }
            }

            else if (listener != null)
            {
                string js = MessageHandler.ConvertToJSON(fieldNamesArray, fieldValuesArray);
                byte[] buffer = Encoding.UTF8.GetBytes(js); // Convert the text to bytes using UTF-8 encoding
                response.ContentLength64 = buffer.Length; // Set the content length in the response
                using (Stream responseStream = response.OutputStream)
                {
                    responseStream.Write(buffer, 0, buffer.Length); // Write the response content to the output stream
                }
            }
            else
            {
                MessageBox.Show("Server is not connected to either socket or postman!");
            }
            sendxml.Hide();
            sendjson.Hide();
        }
        private void submiteditfields_Click(object sender, EventArgs e) //manual field entry
        {
            Thread thread2 = new Thread(sendmessage);
            thread2.Start();
        }

        //Send custom response to client 
        public async void sendmessage()
        {
            await Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    try
                    {
                        int fieldCount = int.Parse(editfields.SelectedItem.ToString());

                        // Retrieve field names and field values from dynamically created TextBoxes
                        List<string> fieldNames = new List<string>();
                        List<string> fieldValues = new List<string>();
                        for (int i = 0; i < fieldCount; i++)
                        {
                            TextBox fieldNameTextBox = this.Controls.Find("fieldNameTextBox_" + i, true).FirstOrDefault() as TextBox;
                            TextBox fieldValueTextBox = this.Controls.Find("fieldValueTextBox_" + i, true).FirstOrDefault() as TextBox;

                            if (fieldNameTextBox != null && fieldValueTextBox != null)
                            {
                                string fieldName = fieldNameTextBox.Text;
                                string fieldValue = fieldValueTextBox.Text;

                                if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    fieldNames.Add(fieldName);
                                    fieldValues.Add(fieldValue);
                                }
                                fieldNamesArray = fieldNames.ToArray();
                                fieldValuesArray = fieldValues.ToArray();


                                label13.Text = "Message send to client";
                            }
                        }
                        sendjson.Show();
                        sendxml.Show();
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() => label13.Text = ex.Message));
                    }
                }));
            });
        }

        private void editfields_SelectedIndexChanged(object sender, EventArgs e) //dropdown for manual fields selection
        {
            submiteditfields.Visible = true;
            int fieldCount = int.Parse(editfields.SelectedItem.ToString());
            label8.Show();
            label10.Show();
            sendjson.Hide();
            sendxml.Hide();
            // Clear existing TextBoxes for field names and field values
            for (int i = this.Controls.Count - 1; i >= 0; i--)
            {
                Control control = this.Controls[i];
                Control option = this.Controls[i];
                if (option is Label && control is TextBox || control.Name.StartsWith("fieldNameTextBox_") || control.Name.StartsWith("fieldValueTextBox_")
                    || option.Name.StartsWith(("Field")))
                {
                    this.Controls.Remove(control);
                    this.Controls.Remove(option);
                    control.Dispose();
                    option.Dispose();

                }
            }

            // Add new TextBoxes for field names and field values
            int startY = label9.Bottom + 50;
            int horizontalOffset = 200; // Adjust this value based on your desired spacing between the textboxes and labels

            for (int i = 0; i < fieldCount; i++)
            {
                Label label = new Label();
                label.Text = "Field_" + (i + 1);
                label.Name = "Field" + i;
                label.Location = new Point(label9.Left + 10, startY + (i * 30) + 6);

                label.Width = 55;
                this.Controls.Add(label);

                TextBox fieldNameTextBox = new TextBox();
                fieldNameTextBox.Name = "fieldNameTextBox_" + i;
                fieldNameTextBox.Location = new Point(label9.Left + 70, startY + (i * 30));
                fieldNameTextBox.Width = label9.Width;
                this.Controls.Add(fieldNameTextBox);

                TextBox fieldValueTextBox = new TextBox();
                fieldValueTextBox.Name = "fieldValueTextBox_" + i;
                fieldValueTextBox.Location = new Point(label9.Left + horizontalOffset, startY + (i * 30));
                fieldValueTextBox.Width = label9.Width;
                this.Controls.Add(fieldValueTextBox);
            }
            int butty = label9.Bottom + 50;
            int buttonYOffset = fieldCount * 30 + 20; // Adjust the offset value as needed
            int buttonX = label9.Left + 70; // Adjust the X coordinate as needed
            int buttonY = butty + buttonYOffset;

            int jsonbutt = label9.Bottom + 50;
            int jsonbuttyoffset = fieldCount * 30 + 20; // Adjust the offset value as needed
            int jsonX = label9.Left + 190; // Adjust the X coordinate as needed
            int jsonY = jsonbutt + jsonbuttyoffset;

            int xmlbutt = label9.Bottom + 50;
            int xmlbuttyoffset = fieldCount * 30 + 20; // Adjust the offset value as needed
            int xmlX = label9.Left + 260; // Adjust the X coordinate as needed
            int xmlY = xmlbutt + xmlbuttyoffset;

            // Set the new position of the button
            submiteditfields.Location = new Point(buttonX, buttonY);
            sendjson.Location = new Point(jsonX, jsonY);
            sendxml.Location = new Point(xmlX, xmlY);

        }
        //Append response with custom entered fields
        private string addmanual(string data)
        {
            if (data == "")
            {
                MessageBox.Show("Please enter any field");
                return "";
            }

            if (MessageHandler.IsJson(data) && data.StartsWith("{"))
            {
                string jsonRequest = "";
                string str = "";
                string text = "";


                string jsonResponse = data;
                jsonRequest = richTextBox2.Text;

                // Remove the last curly bracket from the JSON response
                int lastIndex = jsonResponse.LastIndexOf('}');
                if (lastIndex >= 0)
                {
                    jsonResponse = jsonResponse.Remove(lastIndex, 1);
                }

                // Add a comma after the last item of the JSON response (if needed)
                if (!jsonResponse.TrimEnd().EndsWith(","))
                {
                    jsonResponse += ",";
                }

                // Append the selected fields JSON to the JSON response
                jsonResponse += jsonRequest.TrimStart('{');

                // Display the merged JSON in the second richTextBox
                richTextBox2.Text = jsonResponse;
                TrimEmptyLines(richTextBox2);

            }
            else if (MessageHandler.IsXml(data) && data.StartsWith("<"))
            {

                string xmlRequest = "";
                string selectedFields = "";
                string xmlResponse = "";

                // Convert the selected fields to XML format
                data = data.Replace("<Request>", "");
                data = data.Replace("</Request>", "");
                TrimEmptyLines(richTextBox2);
                xmlResponse = richTextBox2.Text;
                // Find the opening and closing tags in richTextBox2
                int startIndex = xmlResponse.IndexOf('<');
                int endIndex = xmlResponse.LastIndexOf('>');
                if (startIndex >= 0 && endIndex >= 0)
                {
                    // Extract the opening and closing tags
                    string openingTag = xmlResponse.Substring(startIndex, endIndex - startIndex + 1);

                    // Find the closing tag's position in richTextBox2
                    int closingTagIndex = xmlResponse.LastIndexOf("</");

                    // Insert the xmlFields after the opening tag and before the closing tag
                    xmlResponse = xmlResponse.Insert(closingTagIndex, data);

                    // Remove the closing tag of richTextBox1
                    int lastIndex = data.LastIndexOf('>');
                    if (lastIndex >= 0)
                    {
                        string fromTag = data.Substring(startIndex, lastIndex - startIndex + 1);
                        richTextBox2.Text = xmlResponse;
                    }
                }

                // Display the merged XML in the second richTextBox

                richTextBox2.Text = "";
                richTextBox2.Text = xmlResponse;
                TrimEmptyLines(richTextBox2);
                executejson = true;
                executexml = true;
                executeiso = true;
                executefix = true;
                executedelimited = true;
            }
            return richTextBox2.Text;
        }
        private void sendjson_Click(object sender, EventArgs e)
        {
            jsonsendmsg(fieldNamesArray, fieldValuesArray);
        }

        private void sendxml_Click(object sender, EventArgs e)
        {
            xmlsendmsg(fieldNamesArray, fieldValuesArray);
        }
        private void populatechecklistboxreq()
        {
            if (MessageHandler.IsJson(richTextBox1.Text) && richTextBox1.Text.StartsWith("{"))
            {

                if (executejsonreq)
                {
                    JObject json = JObject.Parse(richTextBox1.Text);
                    foreach (JProperty item in json.Properties())
                    {
                        // Access the node name and inner text
                        string name = item.Name;
                        JToken value = item.Value;
                        checkedListBox2.Items.Add(name + ":" + value + "&");
                    }

                }
                executejsonreq = false;
            }
            else if (MessageHandler.IsXml(richTextBox1.Text))
            {
                //Working for xml response
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(richTextBox1.Text);

                // Get the root element
                XmlElement rootElement = xmlDoc.DocumentElement;

                if (executexmlreq == true)
                {
                    foreach (XmlNode node in rootElement.ChildNodes)
                    {
                        // Access the node name and inner text
                        string nodeName = node.Name;
                        string nodeValue = node.InnerText;
                        checkedListBox2.Items.Add(nodeName + ":" + nodeValue + "&");
                    }
                    executexmlreq = false;
                }
            }

            else if (richTextBox1.Text.Substring(0, 4) != "0200" && (!richTextBox1.Text.Contains(delimiter)
                && !richTextBox1.Text.Contains('{') && !richTextBox1.Text.Contains('<') && !richTextBox1.Text.Contains("MIT")))
            {
                richTextBox1.Text = fixedlengthtempreq;
                int[] fieldLengths = new int[40];
                for (int i = 0; i < fieldLengths.Length; i++)
                {
                    fieldLengths[i] = 5;
                }

                int ind = 0;

                string[] field = new string[fieldLengths.Length];
                if (executefixreq)
                {
                    for (int i = 0; i < fieldLengths.Length; i++)
                    {
                        if (ind >= 0 && ind < richTextBox1.Text.Length && fieldLengths[i] > 0 && ind < richTextBox1.Text.Length)
                        {
                            string substring = "";
                            if ((ind + fieldLengths[i]) <= richTextBox1.Text.Length)
                            {
                                substring = richTextBox1.Text.Substring(ind, fieldLengths[i]);
                            }
                            else
                            {
                                substring = richTextBox1.Text.Substring(ind);
                            }

                            string paddedSubstring = substring.PadRight(fieldLengths[i], ' ');
                            checkedListBox2.Items.Add(paddedSubstring + '&');
                            checkedListBox2.SetItemChecked(0, true);
                            ind += fieldLengths[i];
                        }
                        else
                        {
                            break;
                        }

                    }
                    executefixreq = false;
                }
            }
            else if (MessageHandler.IsDelimitedMessage(richTextBox1.Text, delimiter))
            {
                string[] fields = richTextBox1.Text.Split(delimiter);
                if (executedelimitedreq)
                {
                    foreach (string field in fields)
                    {
                        checkedListBox2.Items.Add(field + "&");
                    }
                    executedelimitedreq = false;
                }
            }

            else if (ISO.IsISO8583Format(richTextBox1.Text))
            {
                if (executeisoreq)
                {
                    string[] DE = getDEreq();

                    for (int i = 0; i < DE.Length; i++)
                    {
                        if (i == 129)
                        {
                            Console.WriteLine("MIT: " + DE[i]);
                        }
                        if (DE[i] != null && i != 129)
                        {
                            checkedListBox2.Items.Add("Bit- " + i + " , " + ISO.gethashvalue(i) + ": " + DE[i] + "&");
                        }
                    }

                    executeisoreq = false;
                }
            }
        }
        private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            populatechecklistboxreq();
        }

        private async void UpdatedRequestbtn_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                selectreqbtt();
            });
        }

        private void Selectallreq_Click(object sender, EventArgs e)
        {
            CheckAllItemsreq(checkedListBox2);
        }

        private void deletereq_Click(object sender, EventArgs e)
        {
            if (checkedListBox2.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please Select any item!");
                return;
            }
            DialogResult choose = MessageBox.Show("Do you want to delete selected fields?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (choose == DialogResult.No)
            {
                return;
            }
            else if (choose == DialogResult.Yes)
            {
                // Use a for loop to safely remove items while iterating in reverse
                for (int i = checkedListBox2.CheckedItems.Count - 1; i >= 0; i--)
                {
                    string item = checkedListBox2.CheckedItems[i].ToString();
                    checkedListBox2.Items.Remove(item);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        //Apply permanent changes on response for that specific request
        private void permanentchanges_Click(object sender, EventArgs e)
        {
            if (ResponseCheckboxList.Items.Count > 0)
            {
                // Get the last item in the ListBox
                object mostRecentlyStoredItem = ResponseCheckboxList.Items[ResponseCheckboxList.Items.Count - 1];
                string mostRecentlyStoredItemText = mostRecentlyStoredItem.ToString();
                string filepath = ConfigurationManager.AppSettings["FilePath"].ToString();
                mostRecentlyStoredItemText = filepath + mostRecentlyStoredItemText + ".txt";
                File.WriteAllText(mostRecentlyStoredItemText, richTextBox2.Text);
                MessageBox.Show("Response Updated!");
            }
            else
            {
                MessageBox.Show("No valid response file found!");
            }
        }

        //Remove empty lines
        private void TrimEmptyLines(RichTextBox richTextBox)
        {
            string[] lines = richTextBox.Lines;
            List<string> nonEmptyLines = new List<string>();

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    nonEmptyLines.Add(line);
                }
            }

            richTextBox.Text = string.Join("\n", nonEmptyLines);
        }

        //Add selected fields in response
        private void addreqfieldtoresp()
        {
            if (richTextBox1.Text == "")
            {
                MessageBox.Show("Request is empty!");
                return;
            }
            if (checkedListBox2.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please Select any field");
                return;
            }
            string jsonRequest = "";
            string str = "";
            string text = "";

            if (richTextBox1.Text == "")
            {
                MessageBox.Show("Request not available");
                return;
            }
            if (MessageHandler.IsJson(richTextBox2.Text) && richTextBox2.Text.StartsWith("{"))
            {
                foreach (string item in checkedListBox2.CheckedItems)
                {
                    str += item;
                    checkedListBox1.Items.Add(item);
                }
                text = str.TrimEnd('&');
                JObject selectedFieldObject = ConvertSelectedItemToJson(text);
                jsonRequest = selectedFieldObject.ToString();

                // Convert the selected items to JSON format
                string jsonResponse = richTextBox2.Text;

                // Remove the last curly bracket from the JSON response
                int lastIndex = jsonResponse.LastIndexOf('}');
                if (lastIndex >= 0)
                {
                    jsonResponse = jsonResponse.Remove(lastIndex, 1);
                }

                // Add a comma after the last item of the JSON response (if needed)
                if (!jsonResponse.TrimEnd().EndsWith(","))
                {
                    jsonResponse += ",";
                }

                // Append the selected fields JSON to the JSON response
                jsonResponse += jsonRequest.TrimStart('{');


                // Display the merged JSON in the second richTextBox
                richTextBox2.Text = jsonResponse;

                foreach (object item in checkedListBox2.CheckedItems)
                {
                    foreach (var name in selectedFieldObject.Properties())
                    {
                        string fieldname = item.ToString().Split(':')[0];  //fieldname here is checklistbox fieldname
                        string fieldvalue = item.ToString().Split(':')[1];

                        if (fieldname == name.Name)
                        {
                            richTextBox2.Text.Replace(name.Value.ToString(), fieldvalue);
                        }
                    }
                }
                JObject js = JObject.Parse(richTextBox2.Text);
                JObject cleanedObject = new JObject();

                checkedListBox1.Items.Clear();
                foreach (var property in js.Properties())
                {
                    checkedListBox1.Items.Add(property.Name + ":" + property.Value + '&');
                    if (!cleanedObject.ContainsKey(property.Name))
                    {
                        cleanedObject.Add(property.Name, property.Value);
                    }
                }
                js.RemoveAll();
                richTextBox2.Text = cleanedObject.ToString();
                foreach (var property in cleanedObject.Properties())
                {
                    js.Add(property.Name, property.Value);
                }
                TrimEmptyLines(richTextBox2);
                using (JsonDocument document = JsonDocument.Parse(richTextBox2.Text))
                {
                    System.Text.Json.JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                executejsonreq = true;
                executexmlreq = true;
                executeisoreq = true;
                executefixreq = true;
                executedelimitedreq = true;

                executejson = true;
                executexml = true;
                executeiso = true;
                executefix = true;
                executedelimited = true;
                //checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);
                //checkedListBox2_SelectedIndexChanged(null, EventArgs.Empty);
            }
            else if (MessageHandler.IsXml(richTextBox2.Text))
            {

                string xmlRequest = "";
                string selectedFields = "";
                string xmlResponse = "";
                string xmlFields = "";
                foreach (string item in checkedListBox2.CheckedItems)
                {
                    selectedFields += item;
                }
                selectedFields = selectedFields.TrimEnd('&');

                // Convert the selected fields to XML format
                xmlFields = ConvertToXml(selectedFields);
                TrimEmptyLines(richTextBox2);
                xmlFields = xmlFields.Replace("<Root>", "");
                xmlFields = xmlFields.Replace("</Root>", "");
                TrimEmptyLines(richTextBox1);
                xmlResponse = richTextBox2.Text;

                // Find the opening and closing tags in richTextBox2
                int startIndex = xmlResponse.IndexOf('<');
                int endIndex = xmlResponse.LastIndexOf('>');
                if (startIndex >= 0 && endIndex >= 0)
                {
                    // Extract the opening and closing tags
                    string openingTag = xmlResponse.Substring(startIndex, endIndex - startIndex + 1);

                    // Find the closing tag's position in richTextBox2
                    int closingTagIndex = xmlResponse.LastIndexOf("</");

                    // Insert the xmlFields after the opening tag and before the closing tag
                    xmlResponse = xmlResponse.Insert(closingTagIndex, xmlFields);

                    // Remove the closing tag of richTextBox1
                    int lastIndex = richTextBox2.Text.LastIndexOf('>');
                    if (lastIndex >= 0)
                    {
                        richTextBox2.Text = xmlResponse;
                    }
                }

                // Display the merged XML in the second richTextBox
                richTextBox2.Text = "";
                richTextBox2.Text = xmlResponse;
                TrimEmptyLines(richTextBox2);
                executejson = true;
                executexml = true;
                executeiso = true;
                executefix = true;
                executedelimited = true;
                executejsonreq = true;
                executexmlreq = true;
                executeisoreq = true;
                executefixreq = true;
                executedelimitedreq = true;
                checkedListBox1.Items.Clear();
                checkedListBox1_SelectedIndexChanged(null, EventArgs.Empty);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(richTextBox2.Text);
                XmlElement rootElement = xmlDoc.DocumentElement;

                foreach (object item in checkedListBox2.CheckedItems)
                {
                    string fieldname = item.ToString().Split(':')[0];
                    string fieldvalue = item.ToString().Split(':')[1].TrimEnd('&');

                    XmlNodeList matchingNodes = rootElement.GetElementsByTagName(fieldname);
                    foreach (XmlNode node in matchingNodes)
                    {
                        node.InnerText = fieldvalue;
                    }
                }

                // Create a new XmlDocument to hold non-duplicate elements
                XmlDocument cleanedDoc = new XmlDocument();
                XmlElement cleanedRoot = cleanedDoc.CreateElement(rootElement.Name);
                cleanedDoc.AppendChild(cleanedRoot);

                foreach (XmlNode node in rootElement.ChildNodes)
                {
                    if (node is XmlElement element && !ContainsElementName(cleanedRoot, element.Name))
                    {
                        XmlElement clonedElement = (XmlElement)element.CloneNode(true);
                        XmlNode importedElement = cleanedDoc.ImportNode(clonedElement, true);
                        cleanedRoot.AppendChild(importedElement);
                    }
                }
                richTextBox2.Text = cleanedDoc.InnerXml;
                XDocument xmlobj = XDocument.Parse(richTextBox2.Text);
                richTextBox2.Text = xmlobj.ToString();

            }
        }
        //Helping function for above function for xml conversion
        static bool ContainsElementName(XmlElement root, string name)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement element && element.Name == name)
                {
                    return true;
                }
            }
            return false;
        }
        private void button2_Click_1(object sender, EventArgs e)
        {

            addreqfieldtoresp();
        }

        //Client connection
        private async void Connect_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    string ip;
                    int port;

                    if (TryGetValidIPAndPort(out ip, out port))
                    {
                        Conn().Wait();
                        break; // Exit the loop and start the server
                    }
                }
            });
        }
        private async Task Conn()
        {
            Invoke(new Action(async () =>
            {
                try
                {
                    // while (true) {              
                    string Ip = txtIp.Text;
                    int Port = Convert.ToInt32(txtPort.Text);
                    client = new TcpClient(Ip, Port);
                    ns = client.GetStream();
                    //  sr = new StreamReader(ns);
                    Invoke(new Action(() => label1.Text = "Connected to Server"));
                    // }
                    if (client != null)
                    {
                        try
                        {
                            // Convert the request string to bytes
                            if (richTextBox1.Text != "")
                            {
                                byte[] requestData = System.Text.Encoding.UTF8.GetBytes(richTextBox1.Text);

                                // Send the request to the server
                                ns.Write(requestData, 0, requestData.Length);
                                int bytesRead = 0;
                                byte[] buffer = new byte[4096];
                                try
                                {
                                    while (true)
                                    {
                                        Thread.Sleep(10);
                                        bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length);
                                        if (bytesRead > 0)
                                        {
                                            string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                            Invoke(new Action(() => richTextBox2.Text = response));
                                            sendResponse();
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(e.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            richTextBox2.Text = "Error: " + ex.Message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => label1.Text = "Error: " + ex.Message));
                }

            }));
        }

        private void serverToolStripMenuItem_Click_1(object sender, EventArgs e) //server side
        {
            Connect.Hide();
            label1.Hide();
            button1.Show();
            btn_startHttpListener.Show();
            checkedListBox1.Show();
            Selectall.Show();
            button6.Show();
            UpdatedResponsebtn.Show();
            parsereq.Hide();
        }

        private void clientToolStripMenuItem_Click_1(object sender, EventArgs e) //client side
        {
            Connect.Show();
            label1.Show();
            button1.Hide();
            btn_startHttpListener.Hide();
            checkedListBox1.Hide();
            Selectall.Hide();
            button6.Hide();
            UpdatedResponsebtn.Hide();
            richTextBox2.Clear();
            parsereq.Show();
            int butty = label9.Bottom + 50;
            int buttonYOffset = fields * 20 + 60; // Adjust the offset value as needed
            int buttonX = label9.Left - 780; // Adjust the X coordinate as needed
            int buttonY = butty + buttonYOffset;
            Connect.Location = new Point(buttonX, buttonY);

            int laby = label9.Bottom + 80;
            int labYOffset = fields * 20 + 70; // Adjust the offset value as needed
            int labX = label9.Left - 780; // Adjust the X coordinate as needed
            int labY = laby + labYOffset;
            label11.Location = new Point(labX, labY);
        }

        //Parse custom client request entered in requestbox into fields
        private void parsereq_Click(object sender, EventArgs e)
        {
            checkedListBox2.Items.Clear();
            if (richTextBox1.Text == "")
            {
                MessageBox.Show("Request not available");
                return;
            }
            if (MessageHandler.IsJson(richTextBox1.Text) || MessageHandler.IsXml(richTextBox1.Text) ||
                ISO.IsISO8583Format(richTextBox1.Text) || MessageHandler.IsDelimitedMessage(richTextBox1.Text, delimiter))
            {
                populatechecklistboxreq();
            }
            else if (richTextBox1.Text.Substring(0, 5) != "     ")
            {
                fixedlengthtempreq = richTextBox1.Text;
                populatechecklistboxreq();
            }
            else
            {
                MessageBox.Show("Invalid Message Type! Cannot be parsed.");
            }


            if (ISO.IsISO8583Format(richTextBox1.Text) || !richTextBox1.Text.Substring(0, 5).Contains(" ")
               || MessageHandler.IsDelimitedMessage(richTextBox1.Text, delimiter))

            {
                button2.Hide();
            }
            else if (MessageHandler.IsJson(richTextBox1.Text) || MessageHandler.IsXml(richTextBox1.Text))
            {
                button2.Show();
            }
        }

        private void permanentchanges_MouseHover(object sender, EventArgs e) //permanent save hover
        {
            templabel.Text = "Update response file permanently";
        }

        private void permanentchanges_MouseLeave(object sender, EventArgs e)
        {
            templabel.Text = "";
        }

        private void button5_MouseHover(object sender, EventArgs e)
        {
            permamentlabel.Text = "Save new file in your system";
        }

        private void button5_MouseLeave(object sender, EventArgs e)
        {
            permamentlabel.Text = "";
        }

        private void Connect_MouseHover(object sender, EventArgs e)
        {
            label11.Text = "Connect To Server";
        }

        private void Connect_MouseLeave(object sender, EventArgs e)
        {
            label11.Text = "";
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            label14.Text = "Send Selected Request Fields to Response";
        }

        private void button2_MouseLeave(object sender, EventArgs e)
        {
            label14.Text = "";
        }

        private void Selectallreq_MouseHover(object sender, EventArgs e)
        {
            selectallreqlabel.Text = "Select all request fields";
        }

        private void Selectallreq_MouseLeave(object sender, EventArgs e)
        {
            selectallreqlabel.Text = "";
        }

        private void deletereq_MouseHover(object sender, EventArgs e)
        {
            deletereqlabel.Text = "Delete selected request fields";
        }

        private void deletereq_MouseLeave(object sender, EventArgs e)
        {
            deletereqlabel.Text = "";
        }

        private void UpdatedRequestbtn_MouseHover(object sender, EventArgs e)
        {
            sendreqlabel.Text = "Send selected request fields to server";
        }

        private void UpdatedRequestbtn_MouseLeave(object sender, EventArgs e)
        {
            sendreqlabel.Text = "";
        }

        private void deletereslabel_MouseHover(object sender, EventArgs e)
        {
            deletereslabel.Text = "Delete selected response fields";
        }

        private void deletereslabel_MouseLeave(object sender, EventArgs e)
        {
            deletereslabel.Text = "";
        }

        private void parsereq_MouseHover(object sender, EventArgs e)
        {
            parsereqlabel.Text = "Break Request into fields";
        }

        private void parsereq_MouseLeave(object sender, EventArgs e)
        {
            parsereqlabel.Text = "";
        }

        private void editfields_MouseHover(object sender, EventArgs e)
        {
            sendmanuallabel.Text = "Send custom response to client";
        }

        private void editfields_MouseLeave(object sender, EventArgs e)
        {
            sendmanuallabel.Text = "";
        }

        public void sendResponse()
        {
            sentAutoResponse();
        }

        public void sentAutoResponse()
        {
            // AutoResponse.Checked = true;

            if (AutoResponse.Checked == false)
            {
                Selectall.Show();
                button6.Show();
                UpdatedResponsebtn.Show();
                checkedListBox1.Show();
                return;
            }
            else
            {
                Selectall.Hide();
                button6.Hide();
                UpdatedResponsebtn.Hide();
                checkedListBox1.Hide();
                if (listener != null)
                {
                    string responseText = richTextBox2.Text; // The text you want to send back as the response
                    byte[] buffer = Encoding.UTF8.GetBytes(responseText); // Convert the text to bytes using UTF-8 encoding

                    Stream responseStream = response.OutputStream;
                    try
                    {
                        response.ContentLength64 = buffer.Length; // Set the content length in the response
                    }
                    catch (Exception e11)
                    {
                    }
                    responseStream.Write(buffer, 0, buffer.Length); // Write the response content to the output stream

                }
                if (socketforclients != null)
                {
                    NetworkStream ns = new NetworkStream(socketforclients);
                    sw = new StreamWriter(ns, Encoding.UTF8, 4096, true);
                    string message = richTextBox2.Text;
                    // Convert the message to bytes
                    byte[] messageData = Encoding.UTF8.GetBytes(message);
                    string sendtoclient = (Encoding.UTF8.GetString(messageData));

                    sw.WriteLine(sendtoclient);
                    sw.Flush();
                    sw.Close();
                }
            }
        }
        private void AutoResponse_CheckedChanged(object sender, EventArgs e)
        {

            sentAutoResponse();
        }

        private void AutoResponse_MouseClick(object sender, MouseEventArgs e)
        {
            if (auto % 2 == 0)
            {
                AutoResponse.Checked = false;
                auto++;
            }
            else if (auto % 2 != 0)
            {
                AutoResponse.Checked = true;
                auto++;
            }
        }
    }
}

