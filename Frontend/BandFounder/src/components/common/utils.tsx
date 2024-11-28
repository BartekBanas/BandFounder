import React from "react";

export function sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export function formatMessageWithLinks(message: string): (Array<React.ReactNode>) {
    const urlRegex = /(https?:\/\/\S+)/g;
    return message.split(urlRegex).map((part, index) =>
        urlRegex.test(part) ? (
            <a
                key={index}
                href={part}
                target="_blank"
                rel="noopener noreferrer"
                style={{color: "#00AAFC", textDecoration: "underline"}}
            >
                {part}
            </a>
        ) : (
            part
        )
    );
}