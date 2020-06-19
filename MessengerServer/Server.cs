using MessengerServer.Database_Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessengerServer
{
    internal class Server
    {
        private Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        private IPAddress ip = IPAddress.Parse("127.0.0.1");
        private IPEndPoint ep;
        private Dictionary<string, Socket> clients = new Dictionary<string, Socket>();

        public Server()
        {
            ep = new IPEndPoint(ip, 10240);
            using (MessangerContext context = new MessangerContext())
            {
                if (!context.Users.Any())
                {
                    context.Users.Add(new User() { Name = "General Chat" });
                }
            }
        }

        public void Start()
        {
            s.Bind(ep);
            s.Listen(10);
            try
            {
                while (true)
                {
                    Socket client = s.Accept();
                    Console.WriteLine(client.RemoteEndPoint.ToString());
                    byte[] buffer = new byte[1024];
                    int l = client.Receive(buffer);
                    string login = System.Text.Encoding.ASCII.GetString(buffer, 0, l);
                    if (clients.ContainsKey(login) || login == "General Chat")
                    {
                        client.Send(Encoding.Unicode.GetBytes("Such user already exists"));
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Console.WriteLine("Логин клиента : " + login);
                        clients[login] = client;
                        using (MessangerContext context = new MessangerContext())
                        {
                            var res = context.Users.Where((user) => user.Name == login);
                            if (!res.Any())
                            {
                                context.Users.Add(new User() { Name = login });
                                context.SaveChanges();
                            }
                        }
                        client.Send(Encoding.Unicode.GetBytes("Ok"));
                        Thread.Sleep(50);
                        Task.Run(() => ReceiveMessage(client));
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ReceiveMessage(Socket socket)
        {
            byte[] buffer = new byte[1024];
            int l;
            string login = clients.Where((x) => x.Value == socket).First().Key;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("$123$%^");
            foreach (var item in clients.Keys)
            {
                if(item != login)
                {
                    stringBuilder.Append(item + "^");
                }
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);

            foreach (var sock in clients.Values)
            {
                if(sock != socket)
                {
                    sock.Send(Encoding.Unicode.GetBytes("$123$%^" + login));
                }
                else
                {
                    sock.Send(Encoding.Unicode.GetBytes(stringBuilder.ToString()));
                }
            }
            do
            {
                try
                {
                    l = socket.Receive(buffer);
                    string msg = System.Text.Encoding.Unicode.GetString(buffer, 0, l);
                    login = clients.Where((x) => x.Value == socket).First().Key;

                    string[] msgs = msg.Split('^');
                    string receiver = msgs[0];
                    string messageContent = msgs[1];
                    DateTime date = new DateTime(long.Parse(msgs[2]));

                    foreach (var sock in clients.Values)
                    {
                        sock.Send(Encoding.Unicode.GetBytes($"{receiver}^{login}^{messageContent}^{date.ToString()}"));
                    }
                }
                /*    string receiver;
                    string messageContent;
                    if (msg == "$456$%")
                    {
                        l = socket.Receive(buffer);
                        receiver = System.Text.Encoding.Unicode.GetString(buffer, 0, l);
                        List<Message> messages;
                        using (MessangerContext context = new MessangerContext())
                        {
                            messages = context.Messages.Include(user => user.Receiver).Include(user => user.Sender).Where(mess => mess.Sender.Name == login).Where(mess => mess.Receiver.Name == receiver).ToList();
                            var receivedMessages = context.Messages.Where(mess => mess.Sender.Name == receiver).Where(mess => mess.Receiver.Name == login).ToList();
                            messages.AddRange(receivedMessages);
                            messages.OrderBy(mess => mess.DateOfSendingMessage);
                        }
                        StringBuilder stringBuilder = new StringBuilder();
                        if (messages.Count < 1)
                        {
                            stringBuilder.Append("No messages");
                        }
                        else
                        {
                            foreach (var message in messages)
                            {
                                stringBuilder.Append($"{message.Sender.Name}: {message.MessageContent}; {message.DateOfSendingMessage.ToString()}^");
                                message.IsDelivered = true;
                            }
                        }
                        socket.Send(Encoding.Unicode.GetBytes(stringBuilder.ToString()));
                        using (MessangerContext context = new MessangerContext())
                        {
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        string[] msgs = msg.Split('$');
                        receiver = msgs[0];
                        messageContent = msgs[1];
                        long date = long.Parse(msgs[2]);
                        Message message = new Message();
                        using (MessangerContext context = new MessangerContext())
                        {
                            message.Receiver = context.Users.Where(user => user.Name == receiver).First();
                            message.Sender = context.Users.Where(user => user.Name == login).First();
                            message.IsDelivered = true;
                            message.MessageContent = messageContent;
                            message.DateOfSendingMessage = new DateTime(date);
                            context.Messages.Add(message);
                            context.SaveChanges();
                        }
                        if (receiver == "General Chat")
                        {
                            foreach (var sock in clients.Values)
                            {
                                sock.Send(Encoding.Unicode.GetBytes("$7$%"));
                                Thread.Sleep(50);
                                l = sock.Receive(buffer);
                                msg = Encoding.Unicode.GetString(buffer);
                                if (msg == receiver)
                                {
                                    sock.Send(Encoding.Unicode.GetBytes($"{message.Sender.Name}: {message.MessageContent}; {message.DateOfSendingMessage.ToString()}"));
                                }
                                else
                                {
                                    sock.Send(Encoding.Unicode.GetBytes("$213$%"));
                                    Thread.Sleep(50);
                                    sock.Send(Encoding.Unicode.GetBytes(receiver));
                                    Thread.Sleep(50);
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in clients.Keys)
                            {
                                if (item == receiver)
                                {
                                    clients[item].Send(Encoding.Unicode.GetBytes("$7$%"));
                                    Thread.Sleep(50);
                                    l = clients[item].Receive(buffer);
                                    msg = Encoding.Unicode.GetString(buffer);
                                    if (msg == receiver)
                                    {
                                        clients[item].Send(Encoding.Unicode.GetBytes($"{message.Sender.Name}: {message.MessageContent}; {message.DateOfSendingMessage.ToString()}"));
                                    }
                                    else
                                    {
                                        clients[item].Send(Encoding.Unicode.GetBytes("$213$%"));
                                        Thread.Sleep(50);
                                        clients[item].Send(Encoding.Unicode.GetBytes(receiver));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }*/
                catch (SocketException)
                {
                    login = clients.Where((x) => x.Value == socket).First().Key;
                    clients.Remove(login);
                    Console.WriteLine("User " + login + " disconnected");
                    foreach (var sock in clients.Values)
                    {
                        sock.Send(System.Text.Encoding.Unicode.GetBytes("$321$%^" + login));
                        Thread.Sleep(50);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            } while (true);
        }

        public void Stop()
        {
        }
    }
}