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

        //Словарь пользователей подключенных в данный момент
        private Dictionary<string, Socket> clients = new Dictionary<string, Socket>();

        public Server()
        {
            ep = new IPEndPoint(ip, 10240);
            using (MessangerContext context = new MessangerContext())
            {
                if (!context.Users.Where(user => user.Name == "General Chat").Any())
                {
                    context.Users.Add(new User() { Name = "General Chat" });
                    context.SaveChanges();
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
            byte[] buffer = new byte[5000];
            int l;
            string login = clients.Where((x) => x.Value == socket).First().Key;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("$123$%^");
            foreach (var item in clients.Keys)
            {
                if (item != login)
                {
                    stringBuilder.Append(item + "^");
                }
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);

            foreach (var sock in clients.Values)
            {
                if (sock != socket)
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

                    if (msg.StartsWith("$456$%"))
                    {
                        string[] msgs = msg.Split('^');
                        List<Message> messages;
                        string receiver = msgs[1];
                        using (MessangerContext context = new MessangerContext())
                        {
                            if (receiver == "General Chat")
                            {
                                messages = context.Messages.Include(user => user.Receiver).Include(user => user.Sender).Where(mess => mess.Receiver.Name == receiver).ToList();
                            }
                            else
                            {
                                messages = context.Messages.Include(user => user.Receiver).Include(user => user.Sender).Where(mess => mess.Sender.Name == login).Where(mess => mess.Receiver.Name == receiver).ToList();
                                var receivedMessages = context.Messages.Include(user => user.Receiver).Include(user => user.Sender).Where(mess => mess.Sender.Name == receiver).Where(mess => mess.Receiver.Name == login).ToList();
                                messages.AddRange(receivedMessages);
                            }
                        }
                        /*
                        ToList нужен для корректной сортировке, без него сортирует, но только в формате
                        Sender1 14:36
                        Sender1 14:46
                        Sender2 14:35
                        Sender2 14:37
                        */
                        messages = messages.OrderBy(mess => mess.DateOfSendingMessage).ToList();
                        stringBuilder = new StringBuilder();
                        stringBuilder.Append("$456$%^");
                        if (messages.Count < 1)
                        {
                            stringBuilder.Append("No messages");
                        }
                        else
                        {
                            foreach (var message in messages)
                            {
                                stringBuilder.Append($"{message.Sender.Name}: {message.MessageContent} ({message.DateOfSendingMessage.ToString()})^");
                            }
                            stringBuilder.Remove(stringBuilder.Length - 1, 1);
                        }
                        socket.Send(Encoding.Unicode.GetBytes(stringBuilder.ToString()));
                    }
                    else
                    {
                        string[] msgs = msg.Split('^');
                        string receiver = msgs[0];
                        string messageContent = msgs[1];
                        DateTime date = new DateTime(long.Parse(msgs[2]));

                        Message message = new Message();
                        using (MessangerContext context = new MessangerContext())
                        {
                            message.Receiver = context.Users.Where(user => user.Name == receiver).First();
                            message.Sender = context.Users.Where(user => user.Name == login).First();
                            message.MessageContent = messageContent;
                            message.DateOfSendingMessage = date;
                            context.Messages.Add(message);
                            context.SaveChanges();
                        }

                        if (receiver == "General Chat")
                        {
                            Console.WriteLine(receiver);
                            foreach (var sock in clients.Values)
                            {
                                if (socket != sock)
                                {
                                    sock.Send(Encoding.Unicode.GetBytes($"{receiver}^{login}^{messageContent}^{date.ToString()}"));
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in clients.Keys)
                            {
                                if (item == receiver)
                                {
                                    clients[item].Send(Encoding.Unicode.GetBytes($"{login}^{receiver}^{messageContent}^{date.ToString()}"));
                                    break;
                                }
                            }
                        }
                    }
                }
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
    }
}