package main

import (
	"encoding/binary"
	"fmt"
	"log"
	"net"

	"OmokProtoServer/pkg/manager"
	pb "OmokProtoServer/pkg/messages"

	"google.golang.org/protobuf/proto"
)

func main() {
	listener, err := net.Listen("tcp", ":9090")
	if err != nil {
		log.Fatalf("Failed to listen: %v", err)
	}
	defer listener.Close()
	fmt.Println("Gomoku Server is listening on :9090")

	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Printf("Failed to accept connection: %v", err)
			continue
		}

		go handleConnection(conn)
	}
}

func handleConnection(conn net.Conn) {
	defer conn.Close()
	for {
		lengthBuf := make([]byte, 4)
		_, err := conn.Read(lengthBuf)
		if err != nil {
			log.Printf("Failed to read message length: %v", err)
			return
		}
		length := binary.LittleEndian.Uint32(lengthBuf)

		messageBuf := make([]byte, length)
		_, err = conn.Read(messageBuf)
		if err != nil {
			log.Printf("Failed to read message body: %v", err)
			return
		}

		message := &pb.GomokuMessage{}
		err = proto.Unmarshal(messageBuf, message)
		if err != nil {
			log.Printf("Failed to unmarshal message: %v", err)
			continue
		}

		processMessage(message, &conn)
	}
}

func processMessage(message *pb.GomokuMessage, conn *net.Conn) {
	gomokuManager := manager.GetGomokuManager()

	switch msg := message.Message.(type) {
	case *pb.GomokuMessage_Login:
		playerId := msg.Login.PlayerId
		manager.GetPlayerManager().AddPlayer(playerId, conn)

	case *pb.GomokuMessage_Logout:
		playerId := msg.Logout.PlayerId
		manager.GetPlayerManager().RemovePlayer(playerId)

	case *pb.GomokuMessage_RequestGame:
		gomokuManager.RequestGame(msg.RequestGame.PlayerId)

	case *pb.GomokuMessage_PlaceStone:
		err := gomokuManager.PlaceStone(
			msg.PlaceStone.PlayerId,
			msg.PlaceStone.Position.X,
			msg.PlaceStone.Position.Y,
		)
		if err != nil {
			log.Printf("Error placing stone: %v", err)
		}

	default:
		log.Printf("Unexpected message type: %T", msg)
	}
}
