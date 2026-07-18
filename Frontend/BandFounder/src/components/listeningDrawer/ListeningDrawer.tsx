import React, {FC, useEffect, useState} from 'react';
import {useDisclosure} from '@mantine/hooks';
import {
    CircularProgress,
    Drawer,
    IconButton,
    Tab,
    Tabs,
    ToggleButton,
    ToggleButtonGroup,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import HeadphonesIcon from '@mui/icons-material/Headphones';
import {getUserId} from '../../hooks/authentication';
import {
    getTopArtists,
    getTopTracks,
    SpotifyTimeRange,
    TopArtist,
    TopTrack,
} from '../../api/spotify';
import '../../styles/customScrollbar.css';
import './ListeningDrawer.css';

type ListeningTab = 'artists' | 'songs';

const PERIOD_OPTIONS: { value: SpotifyTimeRange; label: string }[] = [
    {value: 'short_term', label: '1 month'},
    {value: 'medium_term', label: '6 months'},
    {value: 'long_term', label: 'All time'},
];

export const ListeningDrawer: FC = () => {
    const [opened, {open, close}] = useDisclosure(false);
    const [timeRange, setTimeRange] = useState<SpotifyTimeRange>('medium_term');
    const [tab, setTab] = useState<ListeningTab>('artists');
    const [artists, setArtists] = useState<TopArtist[]>([]);
    const [tracks, setTracks] = useState<TopTrack[]>([]);
    const [loading, setLoading] = useState(false);
    const [unavailable, setUnavailable] = useState(false);

    useEffect(() => {
        if (!opened) {
            return;
        }

        let cancelled = false;

        const fetchListeningStats = async () => {
            setLoading(true);
            setUnavailable(false);

            const userId = getUserId();
            const [artistsResult, tracksResult] = await Promise.all([
                getTopArtists(userId, timeRange, 50),
                getTopTracks(userId, timeRange, 50),
            ]);

            if (cancelled) {
                return;
            }

            if (artistsResult === null && tracksResult === null) {
                setArtists([]);
                setTracks([]);
                setUnavailable(true);
            } else {
                setArtists(artistsResult ?? []);
                setTracks(tracksResult ?? []);
            }

            setLoading(false);
        };

        fetchListeningStats();

        return () => {
            cancelled = true;
        };
    }, [opened, timeRange]);

    const handlePeriodChange = (
        _event: React.MouseEvent<HTMLElement>,
        value: SpotifyTimeRange | null
    ) => {
        if (value) {
            setTimeRange(value);
        }
    };

    return (
        <>
            <IconButton color="inherit" onClick={open} aria-label="Listening stats">
                <HeadphonesIcon/>
            </IconButton>

            <Drawer
                className="listening-drawer"
                open={opened}
                onClose={close}
                anchor="right"
            >
                <header className="listening-drawer__header">
                    <h1 className="listening-drawer__title">Listening stats</h1>
                    <IconButton
                        className="listening-drawer__close"
                        onClick={close}
                        aria-label="Close listening stats"
                        size="small"
                    >
                        <CloseIcon/>
                    </IconButton>
                </header>

                <div className="listening-drawer__controls">
                    <ToggleButtonGroup
                        className="listening-drawer__periods"
                        exclusive
                        size="small"
                        value={timeRange}
                        onChange={handlePeriodChange}
                        aria-label="Time period"
                    >
                        {PERIOD_OPTIONS.map((option) => (
                            <ToggleButton key={option.value} value={option.value}>
                                {option.label}
                            </ToggleButton>
                        ))}
                    </ToggleButtonGroup>

                    <Tabs
                        className="listening-drawer__tabs"
                        value={tab}
                        onChange={(_event, value: ListeningTab) => setTab(value)}
                        variant="fullWidth"
                    >
                        <Tab label="Artists" value="artists"/>
                        <Tab label="Songs" value="songs"/>
                    </Tabs>
                </div>

                <div className="listening-drawer__body custom-scrollbar">
                    {loading ? (
                        <div className="listening-drawer__status">
                            <CircularProgress size={28}/>
                        </div>
                    ) : unavailable ? (
                        <p className="listening-drawer__status-text">
                            Connect your Spotify account to see listening stats.
                        </p>
                    ) : tab === 'artists' ? (
                        artists.length > 0 ? (
                            <ol className="listening-drawer__list">
                                {artists.map((artist, index) => (
                                    <li key={artist.id} className="listening-drawer__row">
                                        <span className="listening-drawer__rank">{index + 1}</span>
                                        {artist.imageUrl ? (
                                            <img
                                                className="listening-drawer__avatar"
                                                src={artist.imageUrl}
                                                alt=""
                                                loading="lazy"
                                            />
                                        ) : (
                                            <div className="listening-drawer__avatar listening-drawer__avatar--empty"/>
                                        )}
                                        <div className="listening-drawer__meta">
                                            <span className="listening-drawer__name">{artist.name}</span>
                                            {artist.genres && artist.genres.length > 0 && (
                                                <span className="listening-drawer__genres">
                                                    {artist.genres.join(', ')}
                                                </span>
                                            )}
                                        </div>
                                    </li>
                                ))}
                            </ol>
                        ) : (
                            <p className="listening-drawer__status-text">No top artists for this period.</p>
                        )
                    ) : tracks.length > 0 ? (
                        <ol className="listening-drawer__list">
                            {tracks.map((track, index) => (
                                <li key={track.id} className="listening-drawer__row">
                                    <span className="listening-drawer__rank">{index + 1}</span>
                                    {track.imageUrl ? (
                                        <img
                                            className="listening-drawer__avatar listening-drawer__avatar--cover"
                                            src={track.imageUrl}
                                            alt=""
                                            loading="lazy"
                                        />
                                    ) : (
                                        <div className="listening-drawer__avatar listening-drawer__avatar--empty listening-drawer__avatar--cover"/>
                                    )}
                                    <div className="listening-drawer__meta">
                                        <span className="listening-drawer__name">{track.name}</span>
                                        {track.artistNames.length > 0 && (
                                            <span className="listening-drawer__secondary">
                                                {track.artistNames.join(', ')}
                                            </span>
                                        )}
                                    </div>
                                </li>
                            ))}
                        </ol>
                    ) : (
                        <p className="listening-drawer__status-text">No top songs for this period.</p>
                    )}
                </div>
            </Drawer>
        </>
    );
};
