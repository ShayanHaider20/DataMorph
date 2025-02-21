using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ClientApplication
{
    public static class MessageHandler
    {
        static char delimiter = '|';
        public static bool IsFixedLengthData(string reqdata,string resdata)
        {
            // Check if the data length matches the expected fixed length
            if(!resdata.Contains('<') && !resdata.Contains('{') && !resdata.Contains("MIT") && !resdata.Contains(delimiter)
                && reqdata.Substring(0,4) == resdata.Substring(0,4))
            {
                return true;
            }
            return false;
        }

        public static bool IsDelimitedMessage(string message, char delimiter)
        {
            // Check if the delimiter is present in the message
            if (message.Contains(delimiter) && !message.Contains('{') && !message.Contains('<') && !message.Contains("MIT"))
            {
                // Additional checks to ensure the delimiter is not the first or last character
                int delimiterIndex = message.IndexOf(delimiter);
                return true;
                
            }

            return false;
        }

        static Dictionary<string, string> transCodeSpellings = new Dictionary<string, string>
        {
            { "transcode", "transCode" },
            { "trancode", "transCode" },
            { "tracode", "transCode" },
            { "traCode", "transCode" },
            {"transCode" , "transCode" },
            {"TRANSCODE", "transCode" },
            {"TCode", "transCode" },
            {"trcode", "transCode" },
            {"Code", "transCode" },
            {"CODE", "transCode" },
            {"Processing_Code", "transCode"},
            {"Proc_Code", "transCode"},
            {"PROC_Code", "transCode"},
            {"ProcCode", "transCode"},
            {"ProcessingCode", "transCode"},
            {"PROCESSING_CODE", "transCode"},
            {"PROC_CODE", "transCode"}
            // Add more spellings and their canonical forms as needed
        };

        static string returntranscodespelling(Dictionary<string, string> spelling, string content)
        {
            string nodename = null;

            if (IsJson(content))
            {
                JObject obj = JObject.Parse(content);
                foreach (JProperty property in obj.Properties())
                {
                    // Access the property name and value
                    string propertyName = property.Name;
                    JToken propertyValue = property.Value;
                    foreach (KeyValuePair<string, string> kvp in spelling)
                    {
                        if (kvp.Key == propertyName)
                        {
                            return propertyValue.ToString();
                        }
                    }
                    continue;
                }
            }
            else if (IsXml(content))
            {
                //string transcodeAttribute = null;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);
                XmlElement rootElement = xmlDoc.DocumentElement;
                foreach (XmlNode node in rootElement.ChildNodes)
                {
                    // Access the node name and inner text
                    string nodeName = node.Name;
                    string attributeName = node.Attributes?.Cast<XmlAttribute>().FirstOrDefault()?.Name; //Key
                    // Check if the node has the "Key" attribute and get its value

                    if (node.Attributes != null && node.Attributes[attributeName] != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in spelling)
                        {
                            if (node.Attributes[attributeName].Value == kvp.Key)
                            {
                                return node.InnerText;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        nodename = node.Name;

                        string nodeValue = node.InnerText;
                        foreach (KeyValuePair<string, string> kvp in spelling)
                        {
                            if (kvp.Key == nodename)
                            {
                                return node.InnerText;
                            }
                        }
                    }
                }

            }
            return nodename;
        }

        public static bool IsValidResponse(string requestContent, string responseContent,string filename)
        {
            string isoline = ISO.GetTargetValueFromFile(filename, 1);

            // Check if the request and response are in JSON format
            if (IsJson(requestContent) && IsJson(responseContent) && requestContent.Contains('{') )
            {

                // string transcodereq = FindCanonicalTransCode(requestContent);
                //string transcoderes = FindCanonicalTransCode(responseContent);

                string transcodereq = returntranscodespelling(transCodeSpellings, requestContent);
                string transcoderes = returntranscodespelling(transCodeSpellings, responseContent);

                // Compare the transcode field in the request and response
                if (transcodereq == transcoderes)
                {
                    return true; // Response is valid
                }
            }
            // Check if the request and response are in XML format
            else if (IsXml(requestContent) && IsXml(responseContent)
                && !responseContent.Contains("MIT"))
            {
                XDocument xmlRequest = XDocument.Parse(requestContent);
                string transCodereq = returntranscodespelling(transCodeSpellings, requestContent);
                XDocument xmlRes = XDocument.Parse(responseContent);
                string transCoderes =returntranscodespelling(transCodeSpellings, responseContent);

                if (transCodereq != null && transCoderes != null)
                {

                    // Compare the transcode field in the request and response
                    if (transCodereq == transCoderes)
                    {
                        return true; // Response is valid
                    }
                }
            }

            //check if request and response are in Fixed length format
            else if (!requestContent.Contains(delimiter) && !responseContent.Contains(delimiter)
                 && !responseContent.Contains('<') && !responseContent.Contains('{') && !requestContent.Contains('{')
                 && !responseContent.Contains("MIT"))
            {
                string transcode = requestContent.Substring(0, 5);
                if (!transcode.Contains('-') && transcode == responseContent.Substring(0, 5))
                {
                    return true;
                }
            }


            else if (IsDelimitedMessage(requestContent, '|'))
            {
                if (Delimited.MatchTransCode(requestContent) == Delimited.MatchTransCode(responseContent))
                {
                    return true;
                }

            }

            
            else if (responseContent.StartsWith("MIT") && !requestContent.Contains('<') && !requestContent.Contains('{')
                && !responseContent.Contains('<') && !responseContent.Contains('{'))
            { 
                if(ISO.matchproccode(requestContent))
                {
                    return true;
                }
            }
            return false; // Response is invalid
        }

        public static bool IsJson(string content)
        {
            try
            {
                JToken.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsXml(string content)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static public bool IsJsonContentType(string contentType)
        {
            return MediaTypeHeaderValue.Parse(contentType).MediaType.Equals("application/json");
        }
        static public bool IsXmlContentType(string contentType)
        {
            return MediaTypeHeaderValue.Parse(contentType).MediaType.Equals("application/xml");
        }

        static public string ConvertByteArrayToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        static public string TrimJsonData(string json)
        {
            return json.Trim();
        }

        static public bool IsValidJson(string json)
        {
            try
            {
                JToken.Parse(json);
                return true;
            }
            catch (JsonReaderException j)
            {
                Console.WriteLine("Error Validating Json: " + j.Message);
                return false;
            }
        }
        static public bool IsValidXml(string xml)
        {
            try
            {
                XDocument.Parse(xml);
                return true;
            }
            catch (XmlException xe)
            {
                Console.WriteLine("Error Validating XML: " + xe.Message);
                return false;
            }
        }

        static public string checkreq(string code) //it checks if the transcode entered by client is expected by server
        {
            int select = Convert.ToInt32(code);
            switch (select)
            {
                case 0100:
                    string temp = "C:\\Request 1.txt";
                    return temp;


                case 0117:
                    string temp2 = "C:\\Request 2.txt";
                    return temp2;


                case 0214:
                    string temp3 = "C:\\Request 3.txt";
                    return temp3;

                case 3433:
                    string temp4 = "C:\\reqxml1.txt";
                    return temp4;

                case 3211:
                    string temp5 = "C:\\reqxml2.txt";
                    return temp5;

                default:
                    Console.WriteLine("Invalid code!");
                    return null;
            }
        }

        static public string GetResponseFilePath(string transCode)
        {
            string responseFilePath = null;

            if (Regex.IsMatch(transCode, @"\b0100\b"))
            {
                responseFilePath = "C:\\0100.res.txt";
            }
            else if (Regex.IsMatch(transCode, @"\b0117\b"))
            {
                responseFilePath = "C:\\0117.res.txt";
            }
            else if (Regex.IsMatch(transCode, @"\b0214\b"))
            {
                responseFilePath = "C:\\0214.res.txt";
            }
            else if (Regex.IsMatch(transCode, @"\b3433\b"))
            {
                responseFilePath = "C:\\3433.res.txt";
            }
            else if (Regex.IsMatch(transCode, @"\b3211\b"))
            {
                responseFilePath = "C:\\3211.res.txt";
            }

            return responseFilePath;
        }

        //CONVERTERS
        static public string ConvertJsonToXml(string json)
        {
            try
            {
                var doc = JsonConvert.DeserializeXNode(json.ToString(), "root");
                return doc.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error converting JSON to XML: " + ex.Message);
                return null;
            }
        }

        static public string ConvertXmlToJson(string xml)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented, true);
                return json;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error converting XML to JSON: " + ex.Message);
                return null;
            }
        }
        static public string convertjsontoiso(string request)
        {
            Dictionary<string, int> fieldLengths = new Dictionary<string, int>()

                    {
                        {"Primary Bitmap",16 },
                        {"Secondary Bitmap",16},
                        { "Message Type Indicator MTI", 4 },
                        { "Primary account number PAN", 19 },
                        { "Processing Code", 6 },
                        { "Amount Transaction", 12 },
                        {"Amount settlement",12 },
                        {"Amount cardholder billing", 0},
                        {"Transmission date and time",10 },
                        {"Amount cardholder billing fee",8 },
                         {"Conversion rate, settlement",8 },
                          {"Conversion rate, cardholder billing",8 },
                        { "System trace audit number STAN" ,6},
                         {"Local transaction time",6 },
                          {"Local transaction date",4 },
                          {"Expiration date",4 },
                          {"Settlement date",4 },
                          {"Currency conversion date",4 },
                          {"Capture date",4 },
                        { "Merchant type or merchant category code", 4},
                        {"Acquiring institution (Country code)",3 },
                        {"PAN extended (Country code)",3 },
                        {"Forwarding institution (Country code)",3 },
                        {"Point of service entry mode",3 },
                        {"Application PAN sequence number",3 },
                        {"Function code or network international indentifier(NII)",3 },
                        {"Point of service condition code",2 },
                        {"Point of service capture code",2 },
                        {"Authorizing identification response length",1 },
                        {"Amount, transaction fee",8 },
                        { "Amount, settlement fee" ,8},
                        { "Amount, transaction processing fee" ,8},
                        { "Amount, settlement processing fee" ,8},
                        { "Acquiring institution identification code" ,11},
                        { "Forwarding institution identification code" ,11},
                        { "Primary account number extended" ,28},
                        { "Track 2 data" ,37},
                        { "Track 3 data" ,104},
                        { "Retrieval reference number" ,12},
                        { "Authorization identification response" ,6},    //38
                        { "Response code" ,2},
                        { "Service restriction code" ,3},
                        { "Card acceptor terminal identification" ,8},
                        { "Card acceptor identification code" ,15},
                        { "Card acceptor name/location (1–23 street address, –36 city, –38 state, 39–40 country)" ,40},
                        { "Additional response data" ,25},
                        { "Track 1 data" ,76},
                        { "Additional data (ISO)", 999 },
                         { "Additional data (national)", 999 },
                         { "Additional data (private)", 999 },
                         { "Currency code transaction", 3 },
                         { "Currency code, settlement", 3 },
                         { "Currency code, cardholder billing", 3 },
                         { "Personal identification number data", 64 },
                         { "Security related control information", 16 },
                         { "Additional amounts", 120 }
                    };

            dynamic jsonObject = JsonConvert.DeserializeObject(request);
            // Create a StringBuilder to build the ISO8583 message
            StringBuilder isoMessageBuilder = new StringBuilder();
            Dictionary<string, string> jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(request);
            // Append each field to the ISO8583 message with the appropriate formatting
            foreach (var kvp in fieldLengths)
            {
                string fieldName = kvp.Key;
                int fieldLength = kvp.Value;
                string fieldValue = jsonDict.ContainsKey(fieldName) ? jsonDict[fieldName] : "";
                isoMessageBuilder.Append(fieldValue.PadRight(fieldLength, ' '));
            }
            // Get the final ISO8583 message as a string
            string isoMessage = isoMessageBuilder.ToString();
            return isoMessage;
        }

        static public string convertxmltoiso(string request)
        {
            Dictionary<string, int> fieldLengths = new Dictionary<string, int>()
      {
                        {"Primary Bitmap",16 },
                        {"Secondary Bitmap",16},
                        { "Message Type Indicator MTI", 4 },
                        { "Primary account number PAN", 19 },
                        { "Processing Code", 6 },
                        { "Amount Transaction", 12 },
                        {"Amount settlement",12 },
                        {"Amount cardholder billing", 0},
                        {"Transmission date and time",10 },
                        {"Amount cardholder billing fee",8 },
                         {"Conversion rate, settlement",8 },
                          {"Conversion rate, cardholder billing",8 },
                        { "System trace audit number STAN" ,6},
                         {"Local transaction time",6 },
                          {"Local transaction date",4 },
                          {"Expiration date",4 },
                          {"Settlement date",4 },
                          {"Currency conversion date",4 },
                          {"Capture date",4 },
                        { "Merchant type or merchant category code", 4},
                        {"Acquiring institution (Country code)",3 },
                        {"PAN extended (Country code)",3 },
                        {"Forwarding institution (Country code)",3 },
                        {"Point of service entry mode",3 },
                        {"Application PAN sequence number",3 },
                        {"Function code or network international indentifier(NII)",3 },
                        {"Point of service condition code",2 },
                        {"Point of service capture code",2 },
                        {"Authorizing identification response length",1 },
                        {"Amount, transaction fee",8 },
                        { "Amount, settlement fee" ,8},
                        { "Amount, transaction processing fee" ,8},
                        { "Amount, settlement processing fee" ,8},
                        { "Acquiring institution identification code" ,11},
                        { "Forwarding institution identification code" ,11},
                        { "Primary account number extended" ,28},
                        { "Track 2 data" ,37},
                        { "Track 3 data" ,104},
                        { "Retrieval reference number" ,12},
                        { "Authorization identification response" ,6},    //38
                        { "Response code" ,2},
                        { "Service restriction code" ,3},
                        { "Card acceptor terminal identification" ,8},
                        { "Card acceptor identification code" ,15},
                        { "Card acceptor name/location (1–23 street address, –36 city, –38 state, 39–40 country)" ,40},
                        { "Additional response data" ,25},
                        { "Track 1 data" ,76},
                        { "Additional data (ISO)", 999 },
                         { "Additional data (national)", 999 },
                         { "Additional data (private)", 999 },
                         { "Currency code transaction", 3 },
                         { "Currency code, settlement", 3 },
                         { "Currency code, cardholder billing", 3 },
                         { "Personal identification number data", 64 },
                         { "Security related control information", 16 },
                         { "Additional amounts", 120 }
                    };
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(request);
            StringBuilder isoMessageBuilder = new StringBuilder();
            foreach (var kvp in fieldLengths)
            {
                string fieldName = kvp.Key;
                int fieldLength = kvp.Value;
                XmlNode fieldNode = xmlDoc.SelectSingleNode($"//{fieldName}");
                if (fieldNode == null)
                {
                    continue;
                }
                string fieldValue = fieldNode.InnerText;
                isoMessageBuilder.Append(fieldValue.PadRight(fieldLength, ' '));
            }
            string isoMessage = isoMessageBuilder.ToString();
            return isoMessage;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////

        
        static public void postmantoserverjson( HttpListenerRequest request,   HttpListenerResponse response , ref string req, ref string res)
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            { //Read the request body sent by the client and store it in the requestBody variable
                requestBody = reader.ReadToEnd();
            }
            req = requestBody;

            
        } //postman to json ends here

        static public void postmantoserverxml(HttpListenerRequest request, HttpListenerResponse response, ref string req, ref string res)
        {
            // Read the request body
            string requestBody;
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            { //Read the request body sent by the client and store it in the requestBody variable
                requestBody = reader.ReadToEnd();
            }
           // Console.WriteLine("\nClient Request >> " + requestBody);
            req = requestBody;
          /*  try
            {
                XDocument xmlRequest = XDocument.Parse(requestBody);
                string transCode = xmlRequest.Root.Element("transcode")?.Value;
                string responseFilePath = GetResponseFilePath(transCode);

                string serverXmlRes = File.ReadAllText("C:\\3433.txt");
                string serverXmlRes2 = File.ReadAllText(@"C:\3211.txt");


                // Parse the received message as XML
                if (IsXmlContentType(request.ContentType))
                {
                    string serverXml = File.ReadAllText("C:\\reqxml1.txt"); // reads content of request file
                    string serverXml2 = File.ReadAllText("C:\\reqxml2.txt");

                    // User specifies which request is to be made
                    string checker = checkreq(transCode);
                    string validate = File.ReadAllText(checker);
                    if (IsValidXml(validate))
                    {
                        XElement clientElement = XElement.Parse(requestBody);
                        XElement serverElement = XElement.Parse(validate);

                        bool checkAttr = XNode.DeepEquals(clientElement, serverElement);
                        Console.WriteLine("\nComparing the postman request with file data: ");
                        Console.WriteLine($"The XML elements match: {checkAttr}");

                        // Server sending response to client
                        if (checkAttr && responseFilePath != null)
                        {
                            string responseXml = File.ReadAllText(responseFilePath);

                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.ContentLength64 = Encoding.UTF8.GetByteCount(responseXml);
                            response.ContentType = "application/xml";

                            using (Stream responseStream = response.OutputStream)
                            {
                                byte[] buffer = Encoding.UTF8.GetBytes(responseXml);
                                responseStream.Write(buffer, 0, buffer.Length);
                            }

                            Console.WriteLine("\nResponse Generated: \n");
                            //Console.WriteLine(responseXml);
                            res = responseXml;
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            Console.WriteLine("Postman Request does not match the expected request.\nStatus code: " + response.StatusCode);

                        }
                    }
                    else
                    {
                        Console.WriteLine("Server XML is invalid.");
                        response.StatusCode = (int)HttpStatusCode.BadRequest;

                    }
                }
                else
                {
                    Console.WriteLine("Invalid Content-Type. Expecting application/xml.");
                    response.StatusCode = (int)HttpStatusCode.BadRequest;

                }

            } //try ends

            catch (Exception ex)
            {
                Console.WriteLine("Error processing the request: " + ex.Message);
                response.StatusCode = (int)HttpStatusCode.InternalServerError;

            }
            response.Close();
*/
        } //postman to XML func ends



        public static string ConvertToJSON(string[] fieldName, string[] fieldValue)
        {
            if (fieldName.Length != fieldValue.Length)
            {
                throw new ArgumentException("Number of field names must match the number of field values.");
            }

            // Create a dictionary to store the field-value pairs
            var jsonDict = new Dictionary<string, string>();

            // Add the field-value pairs to the dictionary
            for (int i = 0; i < fieldName.Length; i++)
            {
                string fieldname = fieldName[i];
                string fieldvalue = fieldValue[i];
                jsonDict[fieldname] = fieldvalue;
            }

            // Serialize the dictionary to JSON
            string jsonMessage = JsonConvert.SerializeObject(jsonDict);

            return jsonMessage;
        }

        public static string ConvertToXML(string[] fieldNames, string[] fieldValues)
        {
            // Create an XML document
            XmlDocument xmlDoc = new XmlDocument();

            // Create the root element
            XmlElement rootElement = xmlDoc.CreateElement("Request");
            xmlDoc.AppendChild(rootElement);

            // Create the field elements and add them to the root element
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];
                string fieldValue = fieldValues[i];

                // Create the field element
                XmlElement fieldElement = xmlDoc.CreateElement(fieldName);
                fieldElement.InnerText = fieldValue;
                rootElement.AppendChild(fieldElement);
            }

            // Convert the XML document to string
            StringWriter stringWriter = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
            xmlDoc.WriteTo(xmlWriter);
            string xmlMessage = stringWriter.ToString();

            return xmlMessage;
        }
    }
}
