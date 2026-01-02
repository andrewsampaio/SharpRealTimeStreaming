using Rtsp;
using System;

namespace SharpRTSPServer
{
    /// <summary>
    /// An RTPStream can be a Video Stream, Audio Stream or a Metadata Stream.
    /// ffmpeg -rtsp_transport tcp -i rtsp://camera/stream -vf "drawbox=x=100:y=100:w=200:h=150:color=red@0.8:thickness=3" -c:v libx264 -preset ultrafast -tune zerolatency -f rtsp rtsp://127.0.0.1:8555/live
    /// ffmpeg -stdin 1 -f dshow -rtbufsize 64M -i video = "HD Pro Webcam C920" -vf "sendcmd=filename=pipe\\:0,drawbox" -c:v libx264 -preset ultrafast -tune zerolatency -f rtsp rtsp://localhost:8554/stream1

    /// </summary>
    public class RTPStream
    {
        private static readonly Random _rand = new Random();
        /// <summary>
        /// When true will send out a RTCP packet to match Wall Clock Time to RTP Payload timestamps.
        /// </summary>
        public bool MustSendRtcpPacket { get; set; } = false;

        /// <summary>
        /// Sequence number.
        /// </summary>
        public ushort SequenceNumber { get; set; } = 1;

        /// <summary>
        /// Pair of UDP sockets (data and control) used when sending via UDP.
        /// </summary>
        public IRtpTransport RtpChannel { get; set; }

        // <summary>
        // Time since last RTCP message received - used to spot dead UDP clients.
        // </summary>
        //public DateTime TimeSinceLastRtcpKeepalive { get; set; } = DateTime.UtcNow; 

        /// <summary>
        /// Used in the RTCP Sender Report to state how many RTP packets have been transmitted (for packet loss)
        /// </summary>
        public uint RtpPacketCount { get; set; } = 0;

        /// <summary>
        /// Number of bytes of video that have been transmitted (for average bandwidth monitoring)
        /// </summary>
        public uint OctetCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the Synchronization Source(SSRC) identifier.
        /// This 32-bit value uniquely identifies the source of a stream within an RTP session.
        /// </summary>
        public uint SSRC { get; set; }

        public uint LastRtpTimestamp { get; set; }
        public RTPStream(uint? Ssrc, uint? rtpTimestamp)
        {
            if (Ssrc.HasValue)
            {
                SSRC = Ssrc.Value;
            }
            else
            {
                SSRC = (uint)_rand.Next(0, int.MaxValue);
            }
            if (rtpTimestamp.HasValue)
            {
                LastRtpTimestamp = rtpTimestamp.Value;
            }
            else
            {
                LastRtpTimestamp = (uint)_rand.Next(1, int.MaxValue);
            }
        }
    }
}
