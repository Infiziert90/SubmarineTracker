package main

type WebhookContent struct {
	Username  string  `json:"username,omitempty"`
	Content   string  `json:"content,omitempty"`
	AvatarURL string  `json:"avatar_url,omitempty"`
	Embeds    []Embed `json:"embeds,omitempty"`
}

type Embed struct {
	Title       string `json:"title,omitempty"`
	Url         string `json:"url,omitempty"`
	Description string `json:"description,omitempty"`
	Color       string `json:"color,omitempty"`
}

func NewContent() WebhookContent {
	webhook := WebhookContent{}
	webhook.Username = "[Submarine Tracker]"
	webhook.AvatarURL = "https://raw.githubusercontent.com/Infiziert90/SubmarineTracker/master/SubmarineTracker/images/icon.png"

	return webhook
}
