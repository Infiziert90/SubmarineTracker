package main

import (
    "context"
    "fmt"
    "github.com/jackc/pgx/v5"
    "os"
    "regexp"
    "strings"
    "time"
)

var Conn *pgx.Conn
var Regx *regexp.Regexp

func Connect() {
    var connectionString = fmt.Sprintf("host=%s port=5432 user=%s password=%s database=postgres", os.Getenv("ip"), os.Getenv("username"), os.Getenv("password"))

    var err error
    Conn, err = pgx.Connect(context.Background(), connectionString)
    if err != nil {
        fmt.Fprintf(os.Stderr, "Unable to connect to database: %v\n", err)
        os.Exit(1)
    }

    Regx, _ = regexp.Compile("^.*(discord|discordapp)\\.com\\/api\\/webhooks\\/([\\d]+)\\/([a-z0-9_-]+)$")
}

func GetTimers() []ActiveReturn {
    var invalidTimers = make([]int64, 0)
    var activeTimers = make([]ActiveReturn, 0)

    var timestamp = time.Now().Unix() + 600 // + 10 Min
    var rows, err = Conn.Query(context.Background(), `SELECT id, webhook, content, name, mention, return_time FROM public."SubNotify" WHERE "SubNotify".return_time < $1;`, timestamp)
    if err != nil {
        fmt.Fprintf(os.Stderr, "Query failed: %v\n", err)
        return activeTimers
    }

    for rows.Next() {
        var webhook, content, name string
        var id, mention, returnTime int64
        rows.Scan(&id, &webhook, &content, &name, &mention, &returnTime)

        if !Regx.MatchString(strings.ToLower(webhook)) {
            fmt.Printf("Invalid WebHook: %s\n", webhook)
            invalidTimers = append(invalidTimers, id)

            continue
        }

        var activeReturn = ActiveReturn{
            Id:         id,
            WebhookURL: webhook,
            Content:    content,
            Name:       name,
            Mention:    mention,
            ReturnTime: returnTime,
        }

        activeTimers = append(activeTimers, activeReturn)
    }

    for _, active := range activeTimers {
        fmt.Printf("Deleting (ID: %d)\n", active.Id)
        Conn.Exec(context.Background(), `DELETE FROM public."SubNotify" WHERE id = $1;`, active.Id)
    }

    for _, id := range invalidTimers {
        fmt.Printf("Deleting invalid webhook (ID: %d)\n", id)
        Conn.Exec(context.Background(), `DELETE FROM public."SubNotify" WHERE id = $1;`, id)
    }

    return activeTimers
}
