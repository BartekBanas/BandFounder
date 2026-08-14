import React from 'react';
import GenreLikedSongsTool from '../components/spotifyUtils/GenreLikedSongsTool';
import '../components/spotifyUtils/spotifyUtils.css';

export function SpotifyUtilsPage() {
    return (
        <div className="spotify-utils-page">
            <div className="spotify-utils-page__inner">
                <div>
                    <h1 className="spotify-utils-page__heading">Spotify utilities</h1>
                    <p className="spotify-utils-page__subtitle">
                        Independent tools. Not linked from the rest of the app — open this URL directly.
                    </p>
                </div>
                <GenreLikedSongsTool/>
            </div>
        </div>
    );
}
