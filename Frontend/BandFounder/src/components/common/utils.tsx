import React from "react";

export function sleep(milliseconds: number) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
}

export function formatMessageWithLinks(message: string): React.ReactNode[] {
    const urlRegex = /(https?:\/\/\S+)/g;

    return message.split(urlRegex).map((part, index) => {
        if (urlRegex.test(part)) {
            if (part.includes('https://open.spotify.com/')) {
                return (
                    <React.Fragment key={index}>
                        <a
                            href={part}
                            target="_blank"
                            rel="noopener noreferrer"
                            style={{color: '#00AAFC', textDecoration: 'underline'}}
                        >
                            {part}
                        </a>
                        {convertSpotifyLinkToIframe(part)}
                    </React.Fragment>
                );
            }

            return (
                <a
                    key={index}
                    href={part}
                    target="_blank"
                    rel="noopener noreferrer"
                    style={{color: '#00AAFC', textDecoration: 'underline'}}
                >
                    {part}
                </a>
            );
        }

        return <span key={index}>{part}</span>;
    });
}

export function convertSpotifyLinkToIframe(link: string): React.ReactNode {
    const spotifyLinkRegex = /https:\/\/open.spotify.com\/(artist|track|album|playlist)\/([a-zA-Z0-9]{22})(\?.*)?/;
    const match = link.match(spotifyLinkRegex);

    if (match && match[1] && match[2]) {
        const [_, type, id] = match;
        console.log(`https://open.spotify.com/embed/${type}/${id}?utm_source=generator`);
        return (
            <iframe
                title={`Spotify ${type} ${id}`}
                key={id}
                style={{borderRadius: '12px'}}
                src={`https://open.spotify.com/embed/${type}/${id}?utm_source=generator&theme=1`}
                width="70%"
                height="80"
                allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
                loading="lazy"
            ></iframe>
        );
    }

    return null;
}