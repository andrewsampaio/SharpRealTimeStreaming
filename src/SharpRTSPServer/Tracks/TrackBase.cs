using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpRTSPServer
{
    public abstract class TrackBase : ITrack
    {
        private static readonly Random _rand = new Random();

        /// <summary>
        /// RTP profile.
        /// </summary>
        public RtpProfiles RtpProfile { get; set; } = RtpProfiles.AVP;

        /// <summary>
        /// SSRC for this track. Each track streamed by this server shall have a unique SSRC.
        /// </summary>
        public uint SSRC { get; set; } = (uint)_rand.Next(0, int.MaxValue); 

        public IRtpSender Sink { get; set; } = null;

        public string StreamID { get; set; } = null;

        public abstract string Codec { get; }

        public abstract int ID { get; set; }

        /// <summary>
        /// Payload type. AAC uses a dynamic payload type, which by default we calculate as 96 + track ID.
        /// </summary>
        public abstract int PayloadType { get; set; }

        public abstract bool IsReady { get; }

        public abstract StringBuilder BuildSDP(StringBuilder sdp);

        public abstract (List<Memory<byte>>, List<IMemoryOwner<byte>>) CreateRtpPackets(List<byte[]> samples, uint rtpTimestamp);

        public virtual void FeedInRawSamples(uint rtpTimestamp, List<byte[]> samples)
        {
            if (Sink == null)
                throw new InvalidOperationException("Sink is null!!!");

            if (!Sink.CanAcceptNewSamples(StreamID))
                return;

            if (ID != (int)TrackType.Video && ID != (int)TrackType.Audio && ID != (int)TrackType.Metadata)
                throw new ArgumentOutOfRangeException("ID must be 0 for video, 1 for audio or 2 for metadata");

            var swCreate = Stopwatch.StartNew();
            (List<Memory<byte>> rtpPackets, List<IMemoryOwner<byte>> memoryOwners) = CreateRtpPackets(samples, rtpTimestamp);

            swCreate.Stop();

            var swFeed = Stopwatch.StartNew();

            Sink.FeedInRawRTP(StreamID, ID, rtpTimestamp, rtpPackets);

            swFeed.Stop();

            if (swCreate.ElapsedMilliseconds > 10 ||
                swFeed.ElapsedMilliseconds > 10)
            {
                Console.WriteLine(
                    $"Track={StreamID} Create={swCreate.ElapsedMilliseconds}ms Feed={swFeed.ElapsedMilliseconds}ms Packets={rtpPackets.Count}");
            }
            foreach (var owner in memoryOwners)
            {
                owner.Dispose();
            }
        }
    }
}
