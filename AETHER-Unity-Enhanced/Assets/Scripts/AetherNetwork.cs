using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Aether
{
    [Serializable]
    public class CrewPacket
    {
        public string kind;
        public string id;
        public string instanceId;
        public string crewId;
        public string sender;
        public string utc;
        public string reason;
        public string alertId;
    }

    // Demonstration-only UDP LAN broadcasts. Not encrypted or authenticated,
    // not assured delivery, and NEVER suitable for life-critical messaging.
    public sealed class AetherNetwork : IDisposable
    {
        public const int Port = 34862;
        public string InstanceId { get; private set; }
        public bool IsAvailable { get; private set; }
        public string Status { get; private set; }
        public event Action<CrewPacket> Received;
        public readonly Dictionary<string, float> SeenCrew = new Dictionary<string, float>();

        private readonly ConcurrentQueue<CrewPacket> incoming = new ConcurrentQueue<CrewPacket>();
        private readonly HashSet<string> seenPackets = new HashSet<string>();
        private UdpClient udp;
        private Thread worker;
        private volatile bool running;

        public AetherNetwork()
        {
            InstanceId = Guid.NewGuid().ToString("N");
            Status = "LOCAL SIMULATION ONLY";
        }

        public void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Status = "LAN UDP UNAVAILABLE ON WEBGL";
            IsAvailable = false;
#else
            try
            {
                udp = new UdpClient();
                udp.ExclusiveAddressUse = false;
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
                udp.Client.ReceiveTimeout = 500;
                udp.EnableBroadcast = true;
                running = true;
                IsAvailable = true;
                Status = "LAN DEMO ACTIVE / UDP " + Port;
                worker = new Thread(Listen) { IsBackground = true, Name = "AetherLAN" };
                worker.Start();
            }
            catch (Exception ex)
            {
                Status = "LOCAL DEMO / LAN ERROR: " + ex.Message;
                IsAvailable = false;
                Dispose();
            }
#endif
        }

        private void Listen()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] bytes = udp.Receive(ref remote);
                    if (bytes.Length > 8192) continue;
                    CrewPacket packet = JsonUtility.FromJson<CrewPacket>(Encoding.UTF8.GetString(bytes));
                    if (packet != null && packet.instanceId != InstanceId && !string.IsNullOrEmpty(packet.kind))
                        incoming.Enqueue(packet);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.TimedOut && running) Debug.LogWarning("AETHER LAN receive: " + ex.Message);
                }
                catch (ObjectDisposedException) { return; }
                catch (Exception ex) { if (running) Debug.LogWarning("AETHER LAN: " + ex.Message); }
            }
        }

        public void Poll()
        {
            CrewPacket packet;
            int handled = 0;
            while (handled++ < 30 && incoming.TryDequeue(out packet))
            {
                if (packet.kind == "HELLO")
                {
                    if (!string.IsNullOrEmpty(packet.crewId)) SeenCrew[packet.crewId] = Time.realtimeSinceStartup;
                    continue;
                }
                if (string.IsNullOrEmpty(packet.id) || !seenPackets.Add(packet.id)) continue;
                if (seenPackets.Count > 1500) seenPackets.Clear();
                if (!string.IsNullOrEmpty(packet.crewId)) SeenCrew[packet.crewId] = Time.realtimeSinceStartup;
                if (Received != null) Received(packet);
            }
        }

        public bool IsOnline(string crewId)
        {
            float seen;
            return SeenCrew.TryGetValue(crewId, out seen) && Time.realtimeSinceStartup - seen < 12f;
        }

        public void Send(string kind, string crewId, string sender, string reason = "", string alertId = "")
        {
            if (!IsAvailable || udp == null) return;
            CrewPacket packet = new CrewPacket
            {
                kind = kind, crewId = crewId, sender = sender, reason = reason,
                alertId = alertId, instanceId = InstanceId, id = Guid.NewGuid().ToString("N"),
                utc = DateTime.UtcNow.ToString("o")
            };
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
                udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, Port));
            }
            catch (Exception ex) { Debug.LogWarning("AETHER LAN send: " + ex.Message); }
        }

        public void Dispose()
        {
            running = false;
            IsAvailable = false;
            try { if (udp != null) udp.Close(); } catch { }
            try { if (worker != null && worker.IsAlive) worker.Join(600); } catch { }
            udp = null;
        }
    }
}
