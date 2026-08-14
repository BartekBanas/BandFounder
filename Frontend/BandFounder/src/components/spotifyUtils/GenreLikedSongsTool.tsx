import React, {useEffect, useState} from 'react';
import {Autocomplete, TextField} from '@mui/material';
import {getGenres} from '../../api/metadata';
import {
    getLikedTracksByGenre,
    LikedTrack,
    LikedTracksByGenreProgress,
    LikedTracksByGenreResult,
    SpotifyUtilsApiError,
} from '../../api/spotify';
import UseSpotifyConnected from '../../hooks/useSpotifyAccountLinked';
import {redirectToSpotifyAuthorizationPage} from '../accountDrawer/spotifyConnection/spotifyConnection';
import {AppLoader} from '../common/AppLoader';
import './spotifyUtils.css';

const MAX_GENRE_FILTER_LENGTH = 50;
const MAX_TRACKS_TO_SCAN = 7000;

export const GenreLikedSongsTool: React.FC = () => {
    const [spotifyLinked, setSpotifyLinked] = useState<boolean | null>(null);
    const [genreOptions, setGenreOptions] = useState<string[]>([]);
    const [genre, setGenre] = useState<string>('');
    const [loading, setLoading] = useState(false);
    const [progress, setProgress] = useState<LikedTracksByGenreProgress | null>(null);
    const [result, setResult] = useState<LikedTracksByGenreResult | null>(null);
    const [needsReconnect, setNeedsReconnect] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    useEffect(() => {
        void UseSpotifyConnected().then(setSpotifyLinked);
        void getGenres()
            .then(setGenreOptions)
            .catch((error) => console.error('Error fetching genres:', error));
    }, []);

    const handleFindSongs = async () => {
        const trimmed = genre.trim();
        if (!trimmed) {
            setErrorMessage('Enter a genre first.');
            return;
        }

        setLoading(true);
        setErrorMessage(null);
        setNeedsReconnect(false);
        setProgress(null);
        setResult(null);

        try {
            const response = await getLikedTracksByGenre(trimmed, setProgress);
            setResult(response);
        } catch (error) {
            if (error instanceof SpotifyUtilsApiError) {
                if (error.status === 403 || error.status === 410) {
                    setNeedsReconnect(true);
                } else if (error.status === 422) {
                    setSpotifyLinked(false);
                } else {
                    setErrorMessage(error.message);
                }
            } else {
                setErrorMessage('Something went wrong while scanning liked songs.');
            }
        } finally {
            setLoading(false);
        }
    };

    if (spotifyLinked === null) {
        return (
            <div className="spotify-utils-tool__loader">
                <AppLoader size={80}/>
            </div>
        );
    }

    if (!spotifyLinked || needsReconnect) {
        return (
            <div className="spotify-utils-tool">
                <h2 className="spotify-utils-tool__title">Liked songs by genre</h2>
                <p className="spotify-utils-tool__hint">
                    {needsReconnect
                        ? 'Reconnect Spotify to grant library access (user-library-read). Disconnect and link again from account settings, or use the button below.'
                        : 'Link your Spotify account first, then reconnect if you linked before this tool existed (needs library access).'}
                </p>
                <button
                    type="button"
                    className="spotify-utils-tool__primary"
                    onClick={redirectToSpotifyAuthorizationPage}
                >
                    Connect Spotify
                </button>
            </div>
        );
    }

    return (
        <div className="spotify-utils-tool">
            <h2 className="spotify-utils-tool__title">Liked songs by genre</h2>
            <p className="spotify-utils-tool__hint">
                Scans your Spotify liked songs and keeps tracks whose artists match the genre
                (case-insensitive contains). The scan checks up to 7,000 songs.
            </p>

            <div className="spotify-utils-tool__field">
                <Autocomplete
                    freeSolo
                    fullWidth
                    options={genreOptions}
                    value={genre}
                    onInputChange={(_, value) => setGenre(value ?? '')}
                    renderInput={(params) => (
                        <TextField
                            {...params}
                            label="Genre"
                            size="small"
                            placeholder="e.g. Dream Pop"
                            slotProps={{
                                htmlInput: {
                                    ...params.inputProps,
                                    maxLength: MAX_GENRE_FILTER_LENGTH,
                                },
                            }}
                        />
                    )}
                />
            </div>

            <div className="spotify-utils-tool__actions">
                <button
                    type="button"
                    className="spotify-utils-tool__primary"
                    onClick={handleFindSongs}
                    disabled={loading || !genre.trim()}
                >
                    {loading ? 'Scanning…' : 'Find songs'}
                </button>
            </div>

            {loading && (
                <div className="spotify-utils-tool__loader">
                    <AppLoader size={64}/>
                    <p className="spotify-utils-tool__hint">
                        {progress
                            ? `Scanned ${formatCount(progress.scannedTrackCount)} songs · ${formatCount(progress.remainingTrackCount)} left to scan · ${formatCount(progress.foundMatchCount)} matches found`
                            : 'Starting liked-song scan…'}
                    </p>
                    {progress && progress.totalAvailable > 0 && (
                        <progress
                            className="spotify-utils-tool__progress"
                            value={progress.scannedTrackCount}
                            max={progress.totalAvailable}
                        />
                    )}
                </div>
            )}

            {errorMessage && <p className="spotify-utils-tool__error">{errorMessage}</p>}

            {result && !loading && (
                <div className="spotify-utils-tool__results">
                    <p className="spotify-utils-tool__meta">
                        {result.tracks.length} match
                        {result.tracks.length === 1 ? '' : 'es'} from {result.scannedTrackCount} scanned
                        {result.remainingTrackCount > 0
                            ? ` · ${formatCount(result.remainingTrackCount)} left unscanned`
                            : ''}
                        {result.truncated
                            ? ` — scan capped at ${formatCount(MAX_TRACKS_TO_SCAN)}; results may be partial`
                            : ''}
                    </p>

                    {result.tracks.length === 0 ? (
                        <p className="spotify-utils-tool__hint">No liked songs matched that genre.</p>
                    ) : (
                        <ul className="spotify-utils-tool__list">
                            {result.tracks.map((track) => (
                                <LikedTrackRow key={track.id} track={track}/>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
};

const formatCount = (count: number) => count.toLocaleString();

const LikedTrackRow: React.FC<{ track: LikedTrack }> = ({track}) => {
    const matchingGenres = Array.from(
        new Set(track.artists.flatMap((artist) => artist.genres ?? []))
    ).slice(0, 6);

    return (
        <li className="spotify-utils-track">
            {track.albumImageUrl ? (
                <img
                    className="spotify-utils-track__art"
                    src={track.albumImageUrl}
                    alt=""
                    width={48}
                    height={48}
                />
            ) : (
                <div className="spotify-utils-track__art spotify-utils-track__art--empty"/>
            )}
            <div className="spotify-utils-track__body">
                <div className="spotify-utils-track__name">{track.name}</div>
                <div className="spotify-utils-track__artists">
                    {track.artists.map((artist) => artist.name).join(', ')}
                </div>
                {matchingGenres.length > 0 && (
                    <div className="spotify-utils-track__genres">{matchingGenres.join(' · ')}</div>
                )}
            </div>
        </li>
    );
};

export default GenreLikedSongsTool;
