package main

import (
	"context"
	"fmt"
	"time"
)

// main - Checks the database for new upcoming entries and sleeps for 5 minutes.
func main() {
	Connect()
	defer Conn.Close(context.Background())

	for {
		var activeTimers = GetTimers()

		for _, active := range activeTimers {
			go HandleReturn(active)
		}

		fmt.Println("Sleeping for 5 minutes")
		time.Sleep(5 * time.Minute)
	}
}
