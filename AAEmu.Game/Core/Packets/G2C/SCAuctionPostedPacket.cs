﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Auction;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCAuctionPostedPacket : GamePacket
{
    private readonly AuctionLot _auctionLot;
    public SCAuctionPostedPacket(AuctionLot auctionLot) : base(SCOffsets.SCAuctionPostedPacket, 1)
    {
        _auctionLot = auctionLot;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_auctionLot);

        return stream;
    }
}
