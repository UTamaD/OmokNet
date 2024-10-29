package manager

import (
	"encoding/binary"
	"errors"
	"sync"

	pb "OmokProtoServer/pkg/messages"

	"google.golang.org/protobuf/proto"
)

type GameRoom struct {
	BlackPlayer string
	WhitePlayer string
	Board       []int32
	CurrentTurn int32
	IsGameOver  bool
	Winner      int32
}

type GomokuManager struct {
	rooms        map[string]*GameRoom // key: playerID
	waitingQueue []string
	mu           sync.RWMutex
}

var (
	gomokuManager *GomokuManager
	gomokuOnce    sync.Once
)

func GetGomokuManager() *GomokuManager {
	gomokuOnce.Do(func() {
		gomokuManager = &GomokuManager{
			rooms:        make(map[string]*GameRoom),
			waitingQueue: make([]string, 0),
		}
	})
	return gomokuManager
}

func makePacket(message *pb.GomokuMessage) []byte {
	data, err := proto.Marshal(message)
	if err != nil {
		return nil
	}

	lengthBuf := make([]byte, 4)
	binary.LittleEndian.PutUint32(lengthBuf, uint32(len(data)))
	return append(lengthBuf, data...)
}
func (gm *GomokuManager) RequestGame(playerID string) {
	gm.mu.Lock()
	defer gm.mu.Unlock()

	// Check if player is already in a game
	if _, exists := gm.rooms[playerID]; exists {
		return
	}

	if len(gm.waitingQueue) > 0 {
		blackPlayer := gm.waitingQueue[0]
		gm.waitingQueue = gm.waitingQueue[1:]

		// Create new game room
		room := &GameRoom{
			BlackPlayer: blackPlayer,
			WhitePlayer: playerID,
			Board:       make([]int32, 225), // 15x15 board
			CurrentTurn: 1,                  // Black starts
			IsGameOver:  false,
			Winner:      0,
		}

		// Map room to both players
		gm.rooms[blackPlayer] = room
		gm.rooms[playerID] = room

		// Notify players about game start
		gameStart := &pb.GomokuMessage{
			Message: &pb.GomokuMessage_GameStart{
				GameStart: &pb.GameStart{
					BlackPlayer: blackPlayer,
					WhitePlayer: playerID,
				},
			},
		}

		packet := makePacket(gameStart)
		GetPlayerManager().Broadcast(packet)

		// Send initial game state
		gm.broadcastGameState(room)
	} else {
		gm.waitingQueue = append(gm.waitingQueue, playerID)
	}
}

func (gm *GomokuManager) PlaceStone(playerID string, x, y int32) error {
	gm.mu.Lock()
	defer gm.mu.Unlock()

	room, exists := gm.rooms[playerID]
	if !exists {
		return errors.New("player not in any game")
	}

	if room.IsGameOver {
		return errors.New("game is already over")
	}

	playerStone := int32(1) // black
	if playerID == room.WhitePlayer {
		playerStone = 2 // white
	}

	if room.CurrentTurn != playerStone {
		return errors.New("not your turn")
	}

	pos := y*15 + x
	if room.Board[pos] != 0 {
		return errors.New("position already occupied")
	}

	room.Board[pos] = playerStone

	if gm.checkWin(room.Board, x, y, playerStone) {
		room.IsGameOver = true
		room.Winner = playerStone

		defer func() {

			delete(gm.rooms, room.BlackPlayer)
			delete(gm.rooms, room.WhitePlayer)
		}()
	}

	room.CurrentTurn = 3 - room.CurrentTurn // Switch between 1 and 2

	gm.broadcastGameState(room)

	return nil
}

func (gm *GomokuManager) broadcastGameState(room *GameRoom) {
	gameState := &pb.GomokuMessage{
		Message: &pb.GomokuMessage_GameState{
			GameState: &pb.GameState{
				Board:       room.Board,
				CurrentTurn: room.CurrentTurn,
				IsGameOver:  room.IsGameOver,
				Winner:      room.Winner,
			},
		},
	}

	packet := makePacket(gameState)
	GetPlayerManager().Broadcast(packet)
}
func (gm *GomokuManager) checkWin(board []int32, x, y int32, stone int32) bool {
	directions := [][2]int32{
		{1, 0},  // horizontal
		{0, 1},  // vertical
		{1, 1},  // diagonal
		{1, -1}, // other diagonal
	}

	for _, dir := range directions {
		count := 1

		// Check in positive direction
		for i := int32(1); i <= 4; i++ {
			newX := x + dir[0]*i
			newY := y + dir[1]*i

			if !gm.isValidPosition(newX, newY) {
				break
			}

			if board[newY*15+newX] != stone {
				break
			}
			count++
		}

		// Check in negative direction
		for i := int32(1); i <= 4; i++ {
			newX := x - dir[0]*i
			newY := y - dir[1]*i

			if !gm.isValidPosition(newX, newY) {
				break
			}

			if board[newY*15+newX] != stone {
				break
			}
			count++
		}

		if count >= 5 {
			return true
		}
	}

	return false
}

func (gm *GomokuManager) isValidPosition(x, y int32) bool {
	return x >= 0 && x < 15 && y >= 0 && y < 15
}
