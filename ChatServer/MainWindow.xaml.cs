using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;



namespace ChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
   

        private const string CRLF = "\r\n";



        private List<TcpClient> clientList;
        private List<Object> clientNames;
        private TcpListener server;
        private int port = 5000;
        private int clientCount;
        private bool listen;



        public MainWindow()
        {
            InitializeComponent();
            clientList = new List<TcpClient>();
            clientNames = new List<Object>();
            clientCount = 0;
            txtConnectedClients.Text = "0";
            btnStop.IsEnabled = false;
            btnSend.IsEnabled = false;
            txtConnectedClients.Text = string.Empty;
            OpenLogFile();

        }

        public void BtnStart_Click(object sender, RoutedEventArgs e)
        {
           


            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnSend.IsEnabled = true;


            try
            {

                // check if entered port value is not correct.
                if (!Int32.TryParse(txtPort.Text, out port))
                {                    
                    txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "You entered an invalid port value. Please enter a new value." + CRLF;
                    return;
                }


                // Start a new thread for listening incoming connections
                Thread t = new Thread(ListenIncomingConnections);
                t.Name = "Server listening thread";
                t.IsBackground = true; // make it background thread
                t.Start(); // start the thread
            }

            catch (Exception ex)
            {
                txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Problem starting server." + CRLF;
                txtMessagescreen.Text += CRLF + ex.ToString();
            }



        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            listen = false;
            txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Server stopped listening on port: " + txtPort.Text + CRLF;

            try
            {
                foreach (TcpClient client in clientList)
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    writer.WriteLine("The server is now offline.... Please try later again..."); // Send to clients that server is now offline
                    writer.Flush();
                    Thread.Sleep(10); // Sleep a little before closing that the message has been sent fully
                    client.Close();
                }

                // clear clientlist when the server goes offline
                clientList.Clear();
                server.Stop();

            }
            catch (Exception ex)
            {
                txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + ex + CRLF;
            }

            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnSend.IsEnabled = false;
            txtConnectedClients.Text = string.Empty;

        }



        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {

            // if the sendMessage box is not empty
            if (txtSendMessage.Text != "")
            {

                try
                {

                    foreach (TcpClient client in clientList)
                    {

                        StreamWriter writer = new StreamWriter(client.GetStream());
                        writer.WriteLine("<Message from server> " + txtSendMessage.Text);
                        writer.Flush();
                        
                    }

                    txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "<Message from server> " + txtSendMessage.Text + CRLF;
                    txtSendMessage.Clear();
                }




                catch (Exception ex)
                {
                    txtMessagescreen.Text += "<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Problem with sending to clients.";
                    txtMessagescreen.Text += ex.ToString();
                }
            }
        }

        private void ListenIncomingConnections()
        {

            try
            {
                //keep looping true to accept incoming connections
                listen = true;
                //create server socket
                server = new TcpListener(IPAddress.Any, port);
                //Start listening 
                server.Start();

                Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Server started. Listening on port " + port + CRLF)));

                while (listen)
                {
             
                    //Blocks until a client connects
                    TcpClient client = server.AcceptTcpClient();
                    Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Incoming client connection accepted" + CRLF)));
                    // Start a client request processing thread
                    Thread t = new Thread(ProcessClientRequest);
                    t.IsBackground = true;
                    t.Start(client);

                }

            }

            catch
            {

                Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Start the server again to allow chatting." + CRLF)));

            }




        }



        private void ProcessClientRequest(object o)
        {
            
            TcpClient client = (TcpClient)o;
            clientList.Add(client); // Add a new client to the clienlist          
            clientCount += 1;
            Dispatcher.BeginInvoke((Action)(() => txtConnectedClients.Text = clientCount.ToString())); // Add +1 to the connected clients counter
            string name = string.Empty;
            string input = string.Empty;

            try
            {

                StreamReader reader = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream());




                while (client.Connected)
                {

                    input = reader.ReadLine();

                
                                // Client tells it has connected
                                if (input.Substring(0, 9) == "Connected")
                                {
                                                                
                                    name = input.Substring(9);
                                    SendList(client);
                                    Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + client.GetHashCode() + ": " + name + " has connected to the server." + CRLF)));
                                    Dispatcher.BeginInvoke((Action)(() => listBox_connectedClients.Items.Add(client.GetHashCode() + ": " + name)));
                                    input = "$conn" + name;
                                    clientNames.Add(name);
                                    Broadcast(input);
                                }
                                else
                                {
                                    Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + "> From client: " + client.GetHashCode() + ": " + input + CRLF)));

                                    // send messages to all connected clients
                                    Broadcast(input);
                                }
                                                                              

                }



            }
            catch (SocketException se)
            {
                Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "Problem with the socket. " + se + CRLF)));
            }
            catch (Exception e)
            {
                //    Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("Problem processing client requests. " + e + CRLF)));

            }

            
            clientList.Remove(client);
            clientNames.Remove(name);
            clientCount -= 1;
            SendDisconnected(name);
           

            Dispatcher.BeginInvoke((Action)(() => txtConnectedClients.Text = clientCount.ToString())); // minus one from the connected clients counter
            Dispatcher.BeginInvoke((Action)(() => listBox_connectedClients.Items.Remove(client.GetHashCode() + ": " + name))); // remove client from the listbox    
         
            Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + client.GetHashCode() + ": " + name + " has disconnected." + CRLF)));
            
            if (clientCount == 0)
            {
                Dispatcher.BeginInvoke((Action)(() => txtConnectedClients.AppendText(string.Empty)));
            }



        }



        private void Broadcast(string input)
        {


            try
            {
                //Broadcast every received message to all clients

                foreach (TcpClient client in clientList)
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    writer.WriteLine(input);
                    writer.Flush();
                }

            }

            catch
            {
                Dispatcher.BeginInvoke((Action)(() => txtMessagescreen.AppendText("<" + DateTime.Now.ToString("dddd, dd.M.yyyy, HH:mm:ss") + ">  " + "An error has occurred with broadcasting." + CRLF)));
            }


        }

        private void OpenLogFile()
        {
            string path = "logfile.txt";
            // Open the logfile when application starts

            // If does not exist, create a new file
            if (!File.Exists(path))
            {
                File.Create(path);

            }

            // Else just read the existing file
            else
            {
                using (StreamReader sr = File.OpenText(path))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s == "")
                        {
                            txtMessagescreen.AppendText(s);

                        }
                        else
                        {
                            txtMessagescreen.AppendText(s + CRLF);

                        }

                    }
                }
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string path = "logfile.txt";
            // Write all messages to the logfile

            string appendText = txtMessagescreen.Text;
            File.WriteAllText(path, appendText);

        }

        private void TxtMessagescreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Scroll to the end of the messagescreen when the screen updates
            txtMessagescreen.ScrollToEnd();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //Scroll to the end of the messagescreen when the app starts
            txtMessagescreen.ScrollToEnd();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {

            if (listBox_connectedClients.SelectedIndex == -1)
            {
                return;
            }

            else
            {
                // For kicking clients from server
                KickedFromServer();
            }

        }


        private void KickedFromServer()
        {
            string kickedClient = listBox_connectedClients.SelectedItem.ToString();


            try

            {


                foreach (TcpClient client in clientList)
                {

                    string hash = client.GetHashCode().ToString(); // make int to string    

                    StreamWriter writer = new StreamWriter(client.GetStream());
                    
                    


                    // If the selected item has the same hashcode as the client you want to kick
                    if (kickedClient.Contains(hash))
                    {
                        //Say something before a client has been kicked
                        writer.WriteLine("You have been kicked from the server. Hasta la vista, baby!");
                        writer.Flush();
                        Thread.Sleep(10); // Sleep a little before closing that the message has been sent fully
                        client.Close();
                        listBox_connectedClients.Items.Remove(kickedClient); // Remove kicked client from the listbox

                    }



                }

            }
            catch 
            {
              

            }



        }

 

        private void SendList(TcpClient client)
        {
          
            foreach (object cName in clientNames)
            {
   
                StreamWriter writer = new StreamWriter(client.GetStream());
                writer.WriteLine("#list" + cName);
                writer.Flush();
                Thread.Sleep(5);
                       
            }
        }

        private void SendDisconnected(string name)
        {
            foreach (TcpClient client in clientList)
            {
                StreamWriter writer = new StreamWriter(client.GetStream());
                writer.WriteLine("£disc" + name);
                writer.Flush();
                
            }
        }
     
    }
}
