#region using block

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using MyProject.RTC.Constants;
using MyProject.RTC.Models;
using XSockets.Core.Common.Socket.Attributes;
using XSockets.Core.Common.Socket.Event.Attributes;
using XSockets.Core.Common.Socket.Event.Interface;
using XSockets.Core.XSocket;
using XSockets.Core.XSocket.Helpers;

#endregion

namespace MyProject.RTC
{
    public class ConnectionBroker : XSocketController, IConnectionBroker
    {
        /// <summary>
        ///     Ctor - setting up connectionlist and open/close events
        /// </summary>
        public ConnectionBroker()
        {
            Connections = new List<IPeerConnection>();
        }

        /// <summary>
        ///     Current PeerConnection Settings
        /// </summary>
        [PersistentProperty]

        /// <summary>
        /// List of PeerConnections that the Peer has connected to
        /// </summary>
        [NoEvent]
        public List<IPeerConnection> Connections { get; set; }

        /// <summary>
        ///     The Peer for this client
        /// </summary>
        [NoEvent]
        public IPeerConnection Peer { get; set; }

        public override async Task OnMessage(IMessage message)
        {
            await this.InvokeTo(p => p.Peer.Context == Peer.Context, message);
        }

        /// <summary>
        ///     Distribute signals (SDP's)
        /// </summary>
        /// <param name="signalingModel"></param>
        public async Task ContextSignal(SignalingModel signalingModel)
        {
            await this.InvokeTo(f => f.ConnectionId == signalingModel.Recipient, signalingModel, Events.Context.Signal);
        }

        public async Task ConnectToContext()
        {
            // Pass the client a list of Peers to Connect
            await this.Invoke(GetConnections(Peer)
                .Where(q => !q.Connections.Contains(Peer)).
                 Select(p => p.Peer).AsMessage(Events.Context.Connect));
        }

        /// <summary>
        ///     Leave a context
        /// </summary>
        public async Task LeaveContext()
        {
            NotifyPeerLost();

            Peer.Context = new Guid();
            await this.Invoke(Peer, Events.Context.Created);
        }

        /// <summary>
        ///     Current client changes context
        /// </summary>
        /// <param name="context"></param>
        public async Task ChangeContext(Guid context)
        {
            Peer.Context = context;

            // await this.InvokeTo<ConnectionBroker>(c => c.Peer.Context == context, this.Find(q => q.Peer.Context == context).Select(p => p.Peer), Events.Context.Changed);
            await this.Invoke(Peer, Events.Context.Changed);
        }

        /// <summary>
        /// </summary>
        /// <param name="recipient"></param>
        public async Task DisconnectPeer(Guid recipient)
        {
            await this.PublishTo(p => p.ConnectionId == recipient, new {Sender = ConnectionId}, Events.Peer.Disconnect);
        }

        /// <summary>
        ///     Notify PeerConnections on the current context that a MediaStream is removed.
        /// </summary>
        /// <param name="streamId"></param>
        public async Task RemoveStream(Guid recipient, string streamId)
        {
            await this.InvokeTo(f => f.Peer.PeerId == recipient, new StreamModel {Sender = ConnectionId, StreamId = streamId}, Events.Stream.Remove);
        }

        public async Task AddStream(string streamId, string description)
        {
            await this.InvokeTo(f => f.Peer.Context == Peer.Context,
                new StreamModel
                {
                    Sender = ConnectionId,
                    StreamId = streamId,
                    Description = description
                }, Events.Stream.Add);
        }

        public async void GetContext()
        {
            await this.Invoke(Peer, Events.Context.Created);
        }

        /// <summary>
        ///     When a client disconnects tell the other clients about the Peer being lost
        /// </summary>
        public override async Task OnClosed()
        {
            NotifyPeerLost();
        }

        private async Task NotifyPeerLost()
        {
            if (Peer == null) return;
            await this.InvokeTo(f => f.Peer.Context == Peer.Context, Peer, Events.Peer.Lost);
        }

        public async Task OfferContext(string a)
        {
            // var p = new {Peer = this.Peer};
            var users =
                this.FindOn<ConnectionBroker>(
                    u => u.Peer.Context == Peer.Context && u.PersistentId != PersistentId);

            foreach (var user in users)
            {
                await user.Invoke(Peer, Events.Context.Offer);
            }
        }

        public override async Task OnOpened()
        {
            var context = Guid.NewGuid();

            if (this.HasParameterKey("ctx"))
            {
                var p = this.GetParameter("ctx");
                context = Guid.Parse(p);
            }

            Peer = new PeerConnection
            {
                Context = context,
                PeerId = ConnectionId
            };

            await this.Invoke(Peer, Events.Context.Created);

            // set up a heartbeat ( fake )
            var t = new Timer();

            t.Interval = 1000 * 15;

            t.Elapsed += (sender, args) => { this.Invoke(new {ts = DateTime.Now.Second}, "ping"); };

            t.Start();
        }

        public async Task PhotoStream(string base64, Guid peerId)
        {
            var recipients = this.Find(p => p.Peer.Context == Peer.Context).ToList();
            recipients.Remove(this);
            await this.InvokeTo(recipients, new {base64, peerId}, "photoStream");
        }

        public Task SetContext(Guid context)
        {
            Peer.Context = context;
            return null;
        }

        private IEnumerable<ConnectionBroker> GetConnections(IPeerConnection peerConnection)
        {
            return this.Find(f => f.Peer.Context == peerConnection.Context).Select(p => p).Except(new List<ConnectionBroker> {this});
        }
    }
}