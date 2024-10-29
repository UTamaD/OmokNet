package manager

import (
	"net"
	"sync"
)

type Player struct {
	ID   string
	Conn *net.Conn
}

type PlayerManager struct {
	players map[string]*Player
	mu      sync.RWMutex
}

var (
	playerManager *PlayerManager
	playerOnce    sync.Once
)

func GetPlayerManager() *PlayerManager {
	playerOnce.Do(func() {
		playerManager = &PlayerManager{
			players: make(map[string]*Player),
		}
	})
	return playerManager
}

func (pm *PlayerManager) AddPlayer(playerID string, conn *net.Conn) {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	pm.players[playerID] = &Player{
		ID:   playerID,
		Conn: conn,
	}
}

func (pm *PlayerManager) RemovePlayer(playerID string) {
	pm.mu.Lock()
	defer pm.mu.Unlock()

	delete(pm.players, playerID)
}

func (pm *PlayerManager) GetPlayer(playerID string) *Player {
	pm.mu.RLock()
	defer pm.mu.RUnlock()

	return pm.players[playerID]
}

func (pm *PlayerManager) Broadcast(data []byte) {
	pm.mu.RLock()
	defer pm.mu.RUnlock()

	for _, player := range pm.players {
		(*player.Conn).Write(data)
	}
}
