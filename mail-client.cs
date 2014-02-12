using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace EmailClient {

    class MessageHeader {
        public int Id;
        public String From;
        public String Subject;

    }

    class ProtocolImplementation {
        protected Socket Socket = null;

        protected

                  void SendToSocket(string request) {
            Byte[] b = Encoding.ASCII.GetBytes(request);
            int len = b.Length;
            Socket.Send(b, len, 0);
        }
        private

                String GetFromSocket(out int bytes) {
            Byte[] b = new Byte[1024];
            if (Socket.Available > 0) {
                bytes = Socket.Receive(b, b.Length, 0);
                return Encoding.ASCII.GetString(b, 0, bytes);
            } else {
                bytes = 0;
                return "";
            }

        }
        protected

                  String GetStringFromSocket() {
            string s = "";
            int bytes;
            do {
                s += GetFromSocket(out bytes);
            } while (bytes > 0);

            return s;
        }
        public

               void ConnectToServer(string server, int port) {
            IPHostEntry he = Dns.GetHostEntry(server);

            foreach(IPAddress add in he.AddressList) {
                IPEndPoint ipe = new IPEndPoint(add, port);
                Socket tmp = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                tmp.Connect(ipe);
                if (tmp.Connected) {
                    Socket = tmp;
                    Socket.ReceiveTimeout = 100;
                    return;
                }
            }
            throw new ArgumentException("Could not connect to the server");
        }
    }

    class Pop3Implementation : ProtocolImplementation {
        public

               void Authenticate(string username, string password) {
 

            String init = GetStringFromSocket();
while (String.IsNullOrEmpty(init)) {
                init = GetStringFromSocket();
            }

            SendToSocket("USER " + username + "\r\n");
            String response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("+OK")) {

                throw new WebException(response);
            }
            SendToSocket("PASS " + password + "\r\n");
            response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("+OK")) {
                throw new WebException(response);
            }
        }
        public

               List<MessageHeader> GetMessageList() {

            SendToSocket("LIST\r\n");
            String listOfMessages = GetStringFromSocket();
            while (String.IsNullOrEmpty(listOfMessages)) {
                listOfMessages = GetStringFromSocket();
            }

            if (!listOfMessages.StartsWith("+OK"))
            {
                throw new WebException(listOfMessages);
            }
            String s= GetStringFromSocket();
	    while (String.IsNullOrEmpty(s)) {
                s= GetStringFromSocket();
            }

	    listOfMessages = s;

            String[] messagesArray = listOfMessages.Split(new[] {
                "\r\n"
            }, StringSplitOptions.RemoveEmptyEntries);

            if (messagesArray.Length == 1) {
                return new List<MessageHeader>();
            }
            List<MessageHeader> list = new List<MessageHeader>();
            for (int i = 1; i < messagesArray.Length; i++) {
                MessageHeader m = new MessageHeader();
                String idNumber = messagesArray[i].Split(new[] {
                    " "
                }, StringSplitOptions.RemoveEmptyEntries)[0];
                try {m.Id = Convert.ToInt32(idNumber);} catch(Exception e) {break;}
                SendToSocket("TOP " + m.Id + " 0\r\n");
                string response = GetStringFromSocket();
                while (String.IsNullOrEmpty(response)) {
                    response = GetStringFromSocket();
                }

                response = GetStringFromSocket();
                while (String.IsNullOrEmpty(response)) {
                    response = GetStringFromSocket();
                }

                string[] headersArray = response.Split(new[] {
                    "\r\n"
                }, StringSplitOptions.RemoveEmptyEntries);

                foreach(string head in headersArray) {
                    if (head.StartsWith("From: ")) {
                        m.From = head.Substring(6);
                    }
                    if (head.StartsWith("Subject: ")) {
                        m.Subject = head.Substring(9);
                    }
                }
                list.Add(m);
            }
            return list;
        }
        public

               String GetMessage(int id) {
            SendToSocket("RETR " + id + "\r\n");
            String message = GetStringFromSocket();
            while (String.IsNullOrEmpty(message)) {
                message = GetStringFromSocket();
            }
            if (!(message.Contains("From:"))){
            message = GetStringFromSocket();
            while (String.IsNullOrEmpty(message)) {
                message = GetStringFromSocket();
            }  
            }

            return message;
        }
        public

               void DeleteMessage(int id) {
            SendToSocket("DELE " + id + "\r\n");
            String response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("+OK")) {
                throw new WebException(response);
            }
        }
        public

               void CloseConnection() {
            SendToSocket("QUIT\r\n");
            Socket.Close(3);
        }
    }

    class SmtpImplementation : ProtocolImplementation {
        

        

        public

               void Start() {
            String response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }
            if (!response.StartsWith("220")) {
                throw new WebException(response);
            }
            SendToSocket("HELO it's e-mail client" + "\r\n");
            response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("250")) {
                throw new WebException(response);
            }
        }
        public

               void SetAddresses(string from, string to) {
            SendToSocket("MAIL FROM:<" + from + ">\r\n");
            String response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("250")) {
                throw new WebException(response);
            }
            SendToSocket("RCPT TO:<" + to + ">\r\n");
            response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("250")) {
                throw new WebException(response);
            }

        }
        public

               void SendData(string data, string from, string to, string subject) {
            SendToSocket("DATA\r\n");
            String response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("354")) {
                throw new WebException(response);
            }

            StringBuilder myData = new StringBuilder();
            myData.Append("DATA ");
            myData.Append(subject + "\r\n");
            myData.Append(data);

            SendToSocket(myData + "\r\n. \r\n");
            response = GetStringFromSocket();
            while (String.IsNullOrEmpty(response)) {
                response = GetStringFromSocket();
            }

            if (!response.StartsWith("250")) {
                throw new WebException(response);
            }
        }

        public

               void CloseConnection() {
            SendToSocket("QUIT\r\n");

            Socket.Close(3);
        }
    }

    class Program {
        static Pop3Implementation _pop3 = new Pop3Implementation();
        static SmtpImplementation _smtp = new SmtpImplementation();
        static string smtp_server;
        static int smtp_port;

        static void OpenSession() {
            Console.WriteLine("POP3 Server:");
            string server = Console.ReadLine();
            Console.WriteLine("POP3 Port:");
            int port = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("SMTP server:");
            smtp_server = Console.ReadLine();
            Console.WriteLine("Port:");
            smtp_port = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Username:");
            string uname = Console.ReadLine();
            Console.WriteLine("Password:");
            string pass = Console.ReadLine();
            _pop3.ConnectToServer(server, port);
            _pop3.Authenticate(uname, pass);
        }

        static void PrintMessageList() {
            List<MessageHeader> lomh;
            lomh = _pop3.GetMessageList();

            foreach(MessageHeader mh in lomh) {
                Console.WriteLine("{0} | {1} | {2}", mh.Id, mh.From, mh.Subject);
            }
        }

        static void GetInstruction() {
            Console.WriteLine("r # - read, d # - delete, q - quit, n - new mail");
            String response = Console.ReadLine();
            if (response == null) throw new ArgumentException();
            String[] splitted = response.Split(new [] {
                " "
            }, StringSplitOptions.RemoveEmptyEntries);
            if (splitted[0] == "r") {
                PrintMail(Convert.ToInt32(splitted[1]));
            }
            if (splitted[0] == "d") {
                DeleteMail(Convert.ToInt32(splitted[1]));
            }
            if (splitted[0] == "q") {
                Quit();
            }
            if (splitted[0] == "n") {
                CreateMail();
            }
        }

        static void PrintMail(int msgId) {
            string msg = _pop3.GetMessage(msgId);
            int x = msg.IndexOf("\r\n."); 
            msg = msg.Substring(0, x);
            Console.WriteLine(msg);
        }

        static void DeleteMail(int msgId) {
            _pop3.DeleteMessage(msgId);
        }

        static void Quit() {
            _pop3.CloseConnection();
            Environment.Exit(0);
        }

        static void CreateMail() {
            _smtp.ConnectToServer(smtp_server, smtp_port);
            try {
                _smtp.Start();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("To: ");
            string to = Console.ReadLine();
            Console.WriteLine("From: "); 
            string from = Console.ReadLine();
            Console.WriteLine("Subject: ");
            string subject = Console.ReadLine();
            Console.WriteLine("Start typing data, double enter to finish");
            string data = "";
            string d;
            string d2 = "";
            do {
                d = Console.ReadLine();
                data += d;
                if (d == "") {
                    d2 = Console.ReadLine();
                    data += d2;
                }
            } while (!(d == "" && d2 == ""));
            try {
                _smtp.SetAddresses(from, to);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            _smtp.SendData(data, from, to, subject);
            _smtp.CloseConnection();
        }

        static void Main(string[] args) {
            OpenSession();
            PrintMessageList();
            while (true) {
                GetInstruction();
            }
        }
    }
}

