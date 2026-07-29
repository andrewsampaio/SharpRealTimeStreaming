using SharpRTSPServer;
using System;
using System.Buffers; 
using System.Collections.Generic;
using System.Text;

namespace SharpRTSPServer;

public class MetadataTrack : TrackBase
{
    private ushort _sequenceNumber = 0;
    private readonly int _clockRate = 90000;

    public override string Codec => "vnd.onvif.metadata";

    // Original lib demands ID 0 (Video) or 1 (Audio). 
    // If you want to use the original lib, please use MetadataTrack from OpenCVGateway.Protocols.Onvif project
    // It uses ID 1 (audio slot) to transmit metadata.
    // In this modified version of the lib we're using ID 2 (an addition rtsp channel specially created for this project).
    public override int ID { get; set; } = 2;

    public override int PayloadType { get; set; } = 107; // Dinamic

    public override bool IsReady => true; // Always ready (doesn't need SPS/PPS)

    public override StringBuilder BuildSDP(StringBuilder sdp)
    {
        // SDP assembly (ONVIF Metadata)
        sdp.AppendLine($"m=application 0 RTP/AVP {PayloadType}");
        sdp.AppendLine($"a=control:trackID={ID}");
        //sdp.AppendLine("a=rtpmap");  
        sdp.AppendLine($"a=rtpmap:{PayloadType} {Codec}/{_clockRate}");
        sdp.AppendLine($"a=fmtp:{PayloadType} decoding=string; charset=UTF-8");
        //sdp.AppendLine($"a=control:/{ID}/metadata");
        //sdp.AppendLine("a=recvonly");


        return sdp;
    }

    // RTP PACKETS 
    
    public override (List<Memory<byte>>, List<IMemoryOwner<byte>>) CreateRtpPackets(List<byte[]> samples, uint rtpTimestamp)
    {
        var rtpPackets = new List<Memory<byte>>();
        var memoryOwners = new List<IMemoryOwner<byte>>();

        // Normally metadata fits one packate (small XML)
        // if it's too big (>1400 bytes), it should be fragmented. No usecases were identified for this at this time.
        
        foreach (var sampleData in samples)
        {
            // Total size = 12 bytes (Header RTP + Payload)
            int packetSize = 12 + sampleData.Length;

            // Memory allocation
            byte[] packetBuffer = new byte[packetSize];

            // --- RTP HEADER (12 Bytes) ---

            // Byte 0: Version (2) | Padding (0) | Extension (0) | CSRC Count (0) -> 0x80
            packetBuffer[0] = 0x80;

            // Byte 1: Marker (1) | PayloadType (107)
            // Marker bit = 1 indicates frame end (always true for XML)
            packetBuffer[1] = (byte)(0x80 | (PayloadType & 0x7F));

            // Byte 2-3: Sequence Number (Big Endian)
            packetBuffer[2] = (byte)(_sequenceNumber >> 8);
            packetBuffer[3] = (byte)(_sequenceNumber & 0xFF);
            _sequenceNumber++;

            // Byte 4-7: Timestamp (Big Endian)
            packetBuffer[4] = (byte)(rtpTimestamp >> 24);
            packetBuffer[5] = (byte)(rtpTimestamp >> 16);
            packetBuffer[6] = (byte)(rtpTimestamp >> 8);
            packetBuffer[7] = (byte)(rtpTimestamp & 0xFF);

            // Byte 8-11: SSRC (Big Endian)
            packetBuffer[8] = (byte)(SSRC >> 24);
            packetBuffer[9] = (byte)(SSRC >> 16);
            packetBuffer[10] = (byte)(SSRC >> 8);
            packetBuffer[11] = (byte)(SSRC & 0xFF);

            // --- PAYLOAD (XML) ---
            Array.Copy(sampleData, 0, packetBuffer, 12, sampleData.Length);

            // Response
            rtpPackets.Add(new Memory<byte>(packetBuffer));
        }

        return (rtpPackets, memoryOwners);
    }
    

}