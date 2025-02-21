using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ClientApplication
{
    public static class Delimited
    {

        static public string MatchTransCode(string request)
        {
            string response = "";
            string[] items = Directory.GetFiles("C:\\", "*res.txt");

            foreach (string item in items)
            {
                string content = File.ReadAllText(item);
                string[] responseFields = content.Split('|');

                // Assuming the transaction code is the first field in the response message
                string responseTransCode = responseFields[0];

                //Console.WriteLine(responseTransCode);
                if (responseTransCode == request.Split('|')[0])
                {
                    response = content;
                    return response;
                }
            }
            
            Console.WriteLine("Relevant Response is not found!");
            return null;
        }
        static public bool matchreq(string socketreq)
        {
            string[] items = Directory.GetFiles("C:\\", "*req.txt");
            foreach (string item in items)

            {
                string content = File.ReadAllText(item);
                string[] requestFields = content.Split('|');

                // Assuming the transaction code is the first field in the response message
                string reqTransCode = requestFields[0];

                if (reqTransCode == socketreq)
                {
                    return true;
                }
            }
            return false;
        }
        static void StartServer()
        {
            // Start the server and listen for incoming connections
            string serverIp = "127.0.0.1"; // Replace with your server IP
            int serverPort = 8888; // Replace with your desired server port
            TcpListener server = new TcpListener(IPAddress.Parse(serverIp), serverPort);
            server.Start();
            Console.WriteLine("Server started. Listening for incoming connections...");

            while (true)
            {
                // Accept a client connection
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

                // Handle the client request in a separate thread
                _ = HandleClientRequest(client); // Discard the returned Task object
            }
        }
        static async Task HandleClientRequest(TcpClient client)
        {
            try
            {
                char delimiter = '|';

                // Get the client's network stream for reading and writing
                NetworkStream stream = client.GetStream();

                // Read the request from the client
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                // Convert the received bytes to a string
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                //Main logic for Parsing
                string[] fields = request.Split(delimiter);
                string TransactionCode = fields[0];
                string account = fields[1];
                string amount = fields[2];
                string currency = fields[3];

                if (matchreq(TransactionCode))
                {
                    // Check if the received request is empty or if the client has disconnected
                    if (string.IsNullOrEmpty(request))
                    {
                        Environment.Exit(0);
                    }

                    // Display the received request
                    Console.WriteLine("\nReceived Request: " + request);
                    Console.WriteLine("Received Request Length: " + request.Length);
                    Console.WriteLine("\nThe Request is valid!");

                    // Split the request into fields based on the delimiter
                    Console.WriteLine("\nParsing our Request:-\n");
                    Console.WriteLine("Field 01: " + TransactionCode);
                    Console.WriteLine("Field 02: " + account);
                    Console.WriteLine("Field 03: " + amount);
                    Console.WriteLine("Field 04: " + currency);
                    Console.WriteLine("\nConverting Delimited Request to JSON:\n");
                    //string jsonRequest = ConvertDelimitedToJson(request, delimiter);
                    //Console.WriteLine(jsonRequest);
                    Console.WriteLine("\nConverting Delimited Request to XML:\n");
                    //string xmlRequest = ConvertDelimitedToXml(request, delimiter);
                    //Console.WriteLine(xmlRequest);
                    Console.WriteLine("\nResponse Generated:-");
                    string res = MatchTransCode(TransactionCode);
                    Console.WriteLine(res);
                    Console.WriteLine("The length of Response: " + res.Length);
                    Console.WriteLine("\n");
                    Console.WriteLine("Converting Delimited Response to JSON:\n");
                    //string jsonRes = ConvertDelimitedToJson(res, delimiter);
                    //Console.WriteLine(jsonRes);
                    Console.WriteLine("\nConverting Delimited Response to JSON:\n");
                    //string XMLRes = ConvertDelimitedToJson(res, delimiter);
                    //Console.WriteLine(XMLRes);

                    // Convert the response string to bytes
                    byte[] responseBytes = Encoding.ASCII.GetBytes(res);

                    // Send the response back to the client
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

                    // Close the client connection
                    //client.Close();
                    Console.WriteLine("Client disconnected: " + client.Client.RemoteEndPoint);
                }
                else
                {
                    Console.WriteLine("The Request is not Found in our files!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client request: " + ex.Message);
            }
        }
        public static string ConvertDelimitedToXml(string[] fields, char delimiter)
        {
            // Create an XML document representing the request
            XElement xmlRequest = new XElement("root");

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = "Field" + (i + 1);
                string fieldValue = fields[i].Trim('&');

                if (fieldValue.Contains(delimiter))
                {
                    string[] fieldValueList = fieldValue.Split(delimiter);
                foreach(string value in fieldValueList)
                    {
                        xmlRequest.Add(new XElement(fieldName, value));
                    }
                }
                else
                {
                    xmlRequest.Add(new XElement(fieldName, fieldValue));
                }
            }

            return xmlRequest.ToString();
        }

        public static string ConvertJsonToDelimitedMessage(string jsonData)
        {
            var delimitedMessage = "";

            // Deserialize the JSON string into a JObject object.
            JObject json = JObject.Parse(jsonData);
            // Iterate through the JObject object and add each key-value pair to the delimited message string.
            foreach (JProperty keyValuePair in json.Properties())
            {
                // Get the value of the key-value pair.
                var value = keyValuePair.Value;

                // Add the key-value pair to the delimited message string.
                delimitedMessage += $"{value}|";
            }

            return delimitedMessage.Substring(0, delimitedMessage.Length - 1);
        }

        static public string ConvertDelimitedToJson(string[] fields, char delimiter)
        {
            // Create a JSON object to represent the request
            JObject jsonObject = new JObject();

            for (int i = 0; i < fields.Length; i++)
            {

                string fieldName = "Field" + (i + 1);
                string fieldValue = fields[i].Trim('&');
                if(fieldValue == null)
                {
                    fieldValue = delimiter.ToString();
                }
                jsonObject[fieldName] = fieldValue;
                
            }

            return jsonObject.ToString();
        }

        public static string ConvertDelimitedToFixLen(string delimited, char delimiter)
        {
            int fieldLength = 5;
            // Split the delimited string into fields
            string[] fields = delimited.Split(delimiter);

            // Create a StringBuilder to hold the fixed-length result
            StringBuilder result = new StringBuilder();

            // Loop through each field and append it to the result with proper padding or truncation
            foreach (string field in fields)
            {
                // Pad or truncate the field to the specified fixed length
                string formattedField = field.PadRight(fieldLength).Substring(0, fieldLength);

                // Append the formatted field to the result
                result.Append(formattedField);
            }

            // Return the final fixed-length string
            return result.ToString();
        }


        static public string ConvertDelimitedToIso(string delimitedMessage, char delimiter)

        {

            int[] fieldLengths = { 4, 19, 6, 4, 10, 6, 4, 3 }; // Field lengths for each ISO8583 field



            // Split the delimited message into individual field values

            string[] fieldValues = delimitedMessage.Split(delimiter);



            // Create a StringBuilder to build the ISO8583 message

            StringBuilder isoMessageBuilder = new StringBuilder();



            // Iterate through the field values and append them to the ISO8583 message

            for (int i = 0; i < fieldLengths.Length; i++)

            {

                string fieldValue = (i < fieldValues.Length) ? fieldValues[i] : "";

                int fieldLength = fieldLengths[i];



                isoMessageBuilder.Append(fieldValue.PadRight(fieldLength, ' '));

            }



            // Get the final ISO8583 message as a string

            string isoMessage = isoMessageBuilder.ToString();



            return isoMessage;

        }

        public static string ConvertXmlToDelimited(string xmlString, char delimiter)
        {
            string text = "";
            // Create an XmlDocument object.
            XmlDocument doc = new XmlDocument();

            // Load the XML string into the XmlDocument object.
            doc.LoadXml(xmlString);

            // Get the root element of the XML document.
            XmlElement root = doc.DocumentElement;

            // Iterate through the child nodes of the root element.
            foreach (XmlNode node in root.ChildNodes)
            {
                // Get the name and value of the node.
                var name = node.Name;
                string value = node.InnerText;
                text += value + delimiter;
            }
           return text.TrimEnd('|');
        }





    }
}
