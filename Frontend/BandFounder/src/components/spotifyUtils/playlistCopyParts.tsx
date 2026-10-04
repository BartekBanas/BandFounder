import React from 'react';
import CheckRoundedIcon from '@mui/icons-material/CheckRounded';
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded';
import QueueMusicRoundedIcon from '@mui/icons-material/QueueMusicRounded';
import {PlaylistContributor, SpotifyPlaylistSummary} from '../../api/spotify';
import {AppLoader} from '../common/AppLoader';

export const formatCount = (count: number) => count.toLocaleString();

export const songs = (count: number) => `${formatCount(count)} ${count === 1 ? 'song' : 'songs'}`;

export function joinNames(names: string[], maxShown = 3): string {
    if (names.length > maxShown) {
        const shown = names.slice(0, maxShown - 1);
        const othersCount = names.length - shown.length;
        return `${shown.join(', ')}, and ${othersCount} others`;
    }

    if (names.length <= 2) {
        return names.join(' and ');
    }

    return `${names.slice(0, -1).join(', ')}, and ${names[names.length - 1]}`;
}

export function contributorLabel(contributor: PlaylistContributor): string {
    return contributor.isCurrentUser ? 'You' : contributor.displayName;
}

export type WizardStep = 'source' | 'contributors' | 'target' | 'copy';

const STEPS: { id: WizardStep; label: string }[] = [
    {id: 'source', label: 'Playlist'},
    {id: 'contributors', label: 'Contributors'},
    {id: 'target', label: 'Target'},
    {id: 'copy', label: 'Copy'},
];

export const WizardStepper: React.FC<{
    current: WizardStep;
    finished: boolean;
    canOpen: (step: WizardStep) => boolean;
    onOpen: (step: WizardStep) => void;
}> = ({current, finished, canOpen, onOpen}) => {
    const currentIndex = finished ? STEPS.length : STEPS.findIndex((step) => step.id === current);

    return (
        <ol className="playlist-copy-stepper" aria-label="Progress">
            {STEPS.map((step, index) => {
                const state = index < currentIndex ? 'done' : index === currentIndex ? 'current' : 'upcoming';
                const clickable = state === 'done' && canOpen(step.id);

                return (
                    <li key={step.id} className={`playlist-copy-stepper__step playlist-copy-stepper__step--${state}`}>
                        <button
                            type="button"
                            className="playlist-copy-stepper__button"
                            disabled={!clickable}
                            aria-current={state === 'current' ? 'step' : undefined}
                            onClick={() => onOpen(step.id)}
                        >
                            <span className="playlist-copy-stepper__dot" aria-hidden>
                                {state === 'done' ? <CheckRoundedIcon fontSize="inherit"/> : index + 1}
                            </span>
                            <span className="playlist-copy-stepper__label">{step.label}</span>
                        </button>
                    </li>
                );
            })}
        </ol>
    );
};

export const PlaylistCover: React.FC<{ imageUrl: string | null; size?: number }> = ({imageUrl, size = 40}) =>
    imageUrl ? (
        <img
            className="playlist-copy-cover"
            src={imageUrl}
            alt=""
            width={size}
            height={size}
            style={{width: size, height: size}}
        />
    ) : (
        <span
            className="playlist-copy-cover playlist-copy-cover--empty"
            style={{width: size, height: size}}
            aria-hidden
        >
            <QueueMusicRoundedIcon fontSize="small"/>
        </span>
    );

export function describePlaylist(playlist: SpotifyPlaylistSummary): string {
    const parts = [
        playlist.ownedByMe ? 'Yours' : playlist.ownerName ? `By ${playlist.ownerName}` : null,
        playlist.collaborative ? 'Collaborative' : null,
        songs(playlist.trackCount),
    ];
    return parts.filter(Boolean).join(' · ');
}

export const PlaylistOptionRow: React.FC<{ playlist: SpotifyPlaylistSummary }> = ({playlist}) => (
    <span className="playlist-copy-option">
        <PlaylistCover imageUrl={playlist.imageUrl}/>
        <span className="playlist-copy-option__body">
            <span className="playlist-copy-option__name">{playlist.name || 'Untitled playlist'}</span>
            <span className="playlist-copy-option__meta">{describePlaylist(playlist)}</span>
        </span>
    </span>
);

export const SourceCard: React.FC<{
    playlist: SpotifyPlaylistSummary;
    onChange?: () => void;
}> = ({playlist, onChange}) => (
    <div className="playlist-copy-source">
        <PlaylistCover imageUrl={playlist.imageUrl} size={56}/>
        <div className="playlist-copy-source__body">
            <a
                className="playlist-copy-source__name"
                href={playlist.url}
                target="_blank"
                rel="noreferrer"
                title="Open in Spotify"
            >
                {playlist.name || 'Untitled playlist'}
            </a>
            <span className="playlist-copy-option__meta">{describePlaylist(playlist)}</span>
        </div>
        {onChange && (
            <button type="button" className="spotify-utils-tool__link" onClick={onChange}>
                Change
            </button>
        )}
    </div>
);

const avatarHue = (seed: string) => {
    let hash = 0;
    for (const char of seed) {
        hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
    }
    return hash % 360;
};

export const ContributorRow: React.FC<{
    contributor: PlaylistContributor;
    totalTracks: number;
    checked: boolean;
    onToggle: () => void;
}> = ({contributor, totalTracks, checked, onToggle}) => {
    const label = contributorLabel(contributor);
    const isUnknown = contributor.id === '';
    const nameIsRawId = !isUnknown && !contributor.isCurrentUser && contributor.displayName === contributor.id;
    const share = totalTracks > 0 ? Math.max(2, Math.round((contributor.trackCount / totalTracks) * 100)) : 0;

    return (
        <li>
            <label className={`playlist-copy-contributor${checked ? ' playlist-copy-contributor--checked' : ''}`}>
                <input
                    type="checkbox"
                    className="playlist-copy-contributor__checkbox"
                    checked={checked}
                    onChange={onToggle}
                />
                <span
                    className="playlist-copy-contributor__avatar"
                    style={{backgroundColor: `hsl(${avatarHue(contributor.id || '?')} 42% 34%)`}}
                    aria-hidden
                >
                    {isUnknown ? '?' : label.charAt(0).toUpperCase()}
                </span>
                <span className="playlist-copy-contributor__body">
                    <span className="playlist-copy-contributor__name">
                        {label}
                        {contributor.isCurrentUser && contributor.displayName && (
                            <span className="playlist-copy-contributor__aside">{contributor.displayName}</span>
                        )}
                    </span>
                    <span className="playlist-copy-contributor__meta">
                        {isUnknown && "Spotify didn't record who added these"}
                        {nameIsRawId && (
                            <>
                                Spotify user id ·{' '}
                                <a
                                    href={`https://open.spotify.com/user/${encodeURIComponent(contributor.id)}`}
                                    target="_blank"
                                    rel="noreferrer"
                                >
                                    see who this is <OpenInNewRoundedIcon sx={{fontSize: 12, verticalAlign: '-1px'}}/>
                                </a>
                            </>
                        )}
                    </span>
                    <span className="playlist-copy-contributor__bar" aria-hidden>
                        <span style={{width: `${share}%`}}/>
                    </span>
                </span>
                <span className="playlist-copy-contributor__count">
                    {songs(contributor.trackCount)}
                    <span className="playlist-copy-contributor__share">{share}% of playlist</span>
                </span>
            </label>
        </li>
    );
};

export type PhaseState = 'pending' | 'active' | 'done';

export const CopyPhaseRow: React.FC<{
    label: string;
    state: PhaseState;
    detail?: string;
    processed?: number;
    total?: number;
}> = ({label, state, detail, processed, total}) => (
    <li className={`playlist-copy-phase playlist-copy-phase--${state}`}>
        <span className="playlist-copy-phase__icon" aria-hidden>
            {state === 'done' && <CheckRoundedIcon fontSize="inherit"/>}
            {state === 'active' && <AppLoader size={14}/>}
        </span>
        <span className="playlist-copy-phase__body">
            <span className="playlist-copy-phase__row">
                <span className="playlist-copy-phase__label">{label}</span>
                {detail && <span className="playlist-copy-phase__detail">{detail}</span>}
            </span>
            {state === 'active' && total !== undefined && total > 0 && (
                <progress
                    className="spotify-utils-tool__progress playlist-copy-phase__progress"
                    value={processed ?? 0}
                    max={total}
                />
            )}
        </span>
    </li>
);
