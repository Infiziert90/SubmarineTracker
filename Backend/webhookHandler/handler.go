package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"time"
)

type ActiveReturn struct {
	Id         int64
	WebhookURL string
	Content    string
	Name       string
	Mention    int64
	ReturnTime int64
}

func HandleReturn(active ActiveReturn) {
	var sleepTime = active.ReturnTime - time.Now().Unix()
	time.Sleep(time.Duration(sleepTime) * time.Second)

	var content = NewContent()
	if active.Mention > 0 {
		content.Content = fmt.Sprintf(`<@%d>`, active.Mention)
	}

	var embed = Embed{}
	embed.Title = active.Name
	embed.Description = active.Content
	embed.Color = "8447519"

	content.Embeds = []Embed{embed}

	SendWebhook(active.WebhookURL, content)
}

func SendWebhook(webhookUrl string, content WebhookContent) {
	payload := new(bytes.Buffer)
	err := json.NewEncoder(payload).Encode(content)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Unable to encode as JSON: %v\n", err)
		return
	}

	resp, err := http.Post(webhookUrl, "application/json", payload)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Unable to send webhook: %v\n", err)
		return
	}

	if resp.StatusCode != 200 && resp.StatusCode != 204 {
		defer resp.Body.Close()

		responseBody, err := io.ReadAll(resp.Body)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Unable to read response body: %v\n", err)
			return
		}

		fmt.Fprintf(os.Stderr, "Error response was: %v\n", fmt.Errorf(string(responseBody)))
		return
	}
}
