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
    //private uint _ssrc = 12345; // Identificador da fonte

    public override string Codec => "vnd.onvif.metadata";

    // A lib exige ID 0 (Video) ou 1 (Audio). 
    // Usamos 1 para ocupar o slot de áudio, mas transmitimos metadados.
    public override int ID { get; set; } = 2;

    public override int PayloadType { get; set; } = 107; // Dinâmico

    public override bool IsReady => true; // Sempre pronto (não precisa de SPS/PPS)

    public override StringBuilder BuildSDP(StringBuilder sdp)
    {
        // Monta o bloco SDP que o VMS lê para saber que é ONVIF Metadata
        sdp.AppendLine($"m=application 0 RTP/AVP {PayloadType}");
        sdp.AppendLine($"a=control:trackID={ID}");
        //sdp.AppendLine("a=rtpmap");  
        sdp.AppendLine($"a=rtpmap:{PayloadType} {Codec}/{_clockRate}");
        sdp.AppendLine($"a=fmtp:{PayloadType} decoding=string; charset=UTF-8");
        //sdp.AppendLine($"a=control:/{ID}/metadata");
        //sdp.AppendLine("a=recvonly");


        return sdp;
    }

    // PACOTES RTP 
    
    public override (List<Memory<byte>>, List<IMemoryOwner<byte>>) CreateRtpPackets(List<byte[]> samples, uint rtpTimestamp)
    {
        var rtpPackets = new List<Memory<byte>>();
        var memoryOwners = new List<IMemoryOwner<byte>>();

        // Metadados geralmente cabem em um único pacote (XML pequeno).
        // Se for muito grande (>1400 bytes), deveria ser fragmentado, mas vamos assumir simples por enquanto.

        foreach (var sampleData in samples)
        {
            // Tamanho total = 12 bytes de Header RTP + Payload
            int packetSize = 12 + sampleData.Length;

            // Aloca memória (usando array simples para facilitar, ou ArrayPool em alta performance)
            byte[] packetBuffer = new byte[packetSize];

            // --- 1. CABEÇALHO RTP (12 Bytes) ---

            // Byte 0: Version (2) | Padding (0) | Extension (0) | CSRC Count (0) -> 0x80
            packetBuffer[0] = 0x80;

            // Byte 1: Marker (1) | PayloadType (107)
            // Marker bit = 1 indica fim do frame (para XML sempre é true)
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

            // --- 2. PAYLOAD (XML) ---
            Array.Copy(sampleData, 0, packetBuffer, 12, sampleData.Length);

            // Adiciona à lista de retorno
            rtpPackets.Add(new Memory<byte>(packetBuffer));
        }

        return (rtpPackets, memoryOwners);
    }
    

}