package main

import (
    "context"
    "fmt"
    "time"
)

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
