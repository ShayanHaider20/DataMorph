using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClientApplication
{
    public static class FixedLength
    {
     
        public static string ConvertFixLenToXml(string fixLen)
        {
            fixLen = Regex.Replace(fixLen, @"\\|\""", "");

            // Define the field lengths and positions
            int fieldLength = 5;
            int MAXFIELDS = 100;

            // Create an array to hold the substrings
            string[] fields = new string[MAXFIELDS];

            // Extract the fields from the fixed-length request
            for (int i = 0; i < MAXFIELDS; i++)
            {
                int startIndex = i * fieldLength;
                if (startIndex >= fixLen.Length)
                {
                    break;
                }

                // Extract the substring
                fields[i] = fixLen.Substring(startIndex, Math.Min(fieldLength, fixLen.Length - startIndex));
            }

            // Check for empty fields
            if (fields.Any(field => field == "N/A"))
            {
                Console.WriteLine("Invalid field(s) detected: N/A");
                return null;
            }

            // Create an XML document representing the request
            XElement xmlRequest = new XElement("Request");
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.IsNullOrEmpty(fields[i]))
                {
                    break;
                }

                xmlRequest.Add(new XElement($"Field{i + 1:00}", fields[i]));
            }

            return xmlRequest.ToString();
        }

        public static string ConvertXmlToFixLen(string xml)
        {
            XElement xmlElement = XElement.Parse(xml);
            string fixedLengthString = "";

            foreach (var element in xmlElement.Elements())
            {
                string field = element.Value.Trim().PadRight(5).Substring(0, 5);
                fixedLengthString += field;
            }

            return fixedLengthString;
        }




        public static List<string> ConvertJsonToFixedLength(string json)
    {
        // Parse the JSON string into a JObject
        JObject data = JObject.Parse(json);

        // Get the number of fields in the JSON object
        int count = data.Properties().Count();

        // Create the fieldLengths array with lengths equal to the number of JSON fields
        int[] fieldLengths = new int[count];
        for (int i = 0; i < count; i++)
        {
            fieldLengths[i] = 5; // Set the fixed length for each field (e.g., 5 characters)
        }

        List<string> fixedLengthFields = new List<string>(count);

        // Iterate over the fields and convert them to fixed-length format
        for (int i = 0; i < count; i++)
        {
            string fieldName = data.Properties().ElementAtOrDefault(i)?.Name;
            string fieldValue = data[fieldName]?.ToString();

            // Trim the field value if it exceeds the desired length
            fieldValue = fieldValue?.Substring(0, Math.Min(fieldValue.Length, fieldLengths[i]));

            // Pad or truncate the field to the desired length, or use spaces if the value is null or empty
            fieldValue = fieldValue?.PadRight(fieldLengths[i], ' ') ?? new string(' ', fieldLengths[i]);

            // Add the field to the list
            fixedLengthFields.Add(fieldValue);
        }

        return fixedLengthFields;
    }



        public static string ConvertFixLenToJson(string fixLen)
        {          
            fixLen = Regex.Replace(fixLen, @"\\|\""", "");

            // Define the field lengths and positions
            int[] fieldLengths = new int[40];
            for (int i = 0; i < fieldLengths.Length; i++)
            {
                fieldLengths[i] = 5;
            }

            // Count the number of fields with non-zero length

            // Extract the fields from the fixed-length request
            int ind = 0;
            string[] field = new string[fieldLengths.Length];
            JObject jsonRequest = new JObject();

            for (int i = 0; i < fieldLengths.Length; i++)
            {
                if (ind >= fixLen.Length)
                {
                    break;
                }

                // Extract the substring
                string substring = fixLen.Substring(ind, fieldLengths[i]).Trim(' ');

                // Count the number of blank spaces in the substring
                int blankSpaceCount = substring.Length - substring.TrimEnd().Length;

                // Pad the JSON field with the same number of blank spaces
                field[i] = substring.PadRight(fieldLengths[i] + blankSpaceCount, ' ');
                ind += fieldLengths[i];

                // Check for empty fields
                if (i == 0 && string.IsNullOrEmpty(field[i].Trim()))
                {
                    return "{}";
                }

                // Create a JSON object to represent the request
                jsonRequest["Field" + (i + 1)] = field[i];
            }

            return jsonRequest.ToString();
        }

        public static string ConvertFixLenToDelimited(string fixLen, char delimiter)
        {
            // Define the field lengths and positions
            int fieldLength = 5;
            int MAXFIELDS = 100;

            // Create an array to hold the fields
            string[] fields = new string[MAXFIELDS];

            // Extract the fields from the fixed-length request
            for (int i = 0; i < MAXFIELDS; i++)
            {
                int startIndex = i * fieldLength;
                if (startIndex >= fixLen.Length)
                {
                    break;
                }

                // Extract the substring and pad or truncate it to be of length 5 characters
                fields[i] = fixLen.Substring(startIndex, Math.Min(fieldLength, fixLen.Length - startIndex))
                                 .PadRight(fieldLength)
                                 .Substring(0, fieldLength);
            }

            // Create a delimited string representation of the fixed-length message
            string delimitedMessage = string.Join(delimiter.ToString(), fields.Where(field => !string.IsNullOrEmpty(field)));

            return delimitedMessage;
        }

        static public string matchtranscode(string request)
        {
            string response = "";

            string[] items = Directory.GetFiles("C:\\", "*res.txt");

            foreach (string item in items)
            {
                string content = File.ReadAllText(item);

                if (content.Substring(1, 5) == request.Substring(1, 5))
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

                if (content.Length == socketreq.Length)
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
                _ = HandleClientRequest(client); //  _ variable is used to discard the returned Task object because we don't need to await or use it further in this example
            }
        }

        static async Task HandleClientRequest(TcpClient client)
        {
            while (true)
            {
                try
                {
                    // Get the client's network stream for reading and writing
                    NetworkStream stream = client.GetStream();

                    // Read the request from the client
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    // Convert the received bytes to a string
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (matchreq(request))
                    {
                        // Check if the received request is empty or if the client has disconnected
                        if (string.IsNullOrEmpty(request))
                            //hello whats up dude?
                        {
                            Environment.Exit(0);
                        }

                        // Display the received request
                        Console.WriteLine("Received Request Length: " + request.Length);
                        Console.WriteLine("\nReceived Request: " + request);
                        Console.WriteLine("\nThe Request is valid!");

                        // Parse the request based on fixed-length logic
                        // Sample message: "01234 ABC123456789012 3456789012 USD"
                        string transCode = request.Substring(1, 5);
                        string accountNumber = request.Substring(6, 15);
                        string amount = request.Substring(21, 10);
                        string currencyCode = request.Substring(31, 3);

                        Console.WriteLine("\nParsing our Request:-\n");
                        Console.WriteLine("Transaction Code: " + transCode);
                        Console.WriteLine("Account Number: " + accountNumber);
                        Console.WriteLine("Amount: " + amount);
                        Console.WriteLine("Currency Code: " + currencyCode);

                        Console.WriteLine("\nResponse Generated:-");
                        string res = matchtranscode(request);
                        Console.WriteLine(res);

                        // Generate a sample response based on theparsed request
                        // string response = $"Transaction Code:{transCode}, Account Number: {accountNumber}, Amount: {amount}, Currency Code: {currencyCode}";

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
                        Console.WriteLine("Invalid Request Received. The length must be fixed i.e. 35");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling client request: " + ex.Message);
                }
            }
        }
    }
}
