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

// ActiveReturn - Database layout of a running timer that is soon to be executed.
type ActiveReturn struct {
	Id          int64
	WebhookURL  string
	Content     string
	Name        string
	Mention     int64
	RoleMention int64
	ReturnTime  int64
}

type RateLimitResponse struct {
	Message    string  `json:"message"`
	RetryAfter float64 `json:"retry_after"`
	Global     bool    `json:"global"`
	ErrorCode  int     `json:"code,omitempty"`
}

// HandleReturn - Sleeps until the time has elapsed and prepares the notification.
func HandleReturn(active ActiveReturn) {
	var sleepTime = active.ReturnTime - time.Now().Unix()
	time.Sleep(time.Duration(sleepTime) * time.Second)

	var content = NewContent()

	var mentionContent = ""
	if active.Mention > 0 {
		mentionContent += fmt.Sprintf(`<@%d>`, active.Mention)
	}

	if active.RoleMention > 0 {
		mentionContent += fmt.Sprintf(`<@&%d>`, active.RoleMention)
	}

	if len(mentionContent) > 0 {
		content.Content = mentionContent
	}

	var embed = Embed{}
	embed.Title = active.Name
	embed.Description = active.Content
	embed.Color = "8447519"

	content.Embeds = []Embed{embed}

	SendWebhook(active.WebhookURL, content)
}

// SendWebhook - Sends the webhook as JSON encoded payload and checks the return value for errors.
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

	// Rate-Limit hit, retry after waiting
	if resp.StatusCode == 429 {
		fmt.Println("Rate limit exceeded")
		var rateLimit RateLimitResponse
		err = json.NewDecoder(resp.Body).Decode(&rateLimit)
		resp.Body.Close()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Unable to convert response to rate-limit json: %v\n", err)
			return
		}

		time.Sleep(time.Duration(rateLimit.RetryAfter * float64(time.Second)))
		SendWebhook(webhookUrl, content)

		return
	}

	if resp.StatusCode != 200 && resp.StatusCode != 204 {
		defer resp.Body.Close()

		responseBody, err := io.ReadAll(resp.Body)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Unable to read response body: %v\n", err)
			return
		}

		fmt.Fprintf(os.Stderr, "Error response was: %s\n", string(responseBody))
		return
	}
}
