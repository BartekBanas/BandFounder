import React, {useCallback, useEffect, useMemo, useRef, useState} from 'react';
import {Autocomplete, createFilterOptions, TextField} from '@mui/material';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded';
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded';
import {
    fetchMyEditablePlaylists,
    parsePlaylistId,
    PlaylistContributorsPreview,
    PlaylistCopyPhase,
    PlaylistCopyProgress,
    PlaylistCopyResult,
    previewPlaylistContributors,
    SpotifyPlaylistSummary,
    SpotifyUtilsApiError,
    streamPlaylistCopy,
} from '../../api/spotify';
import UseSpotifyConnected from '../../hooks/useSpotifyAccountLinked';
import {redirectToSpotifyAuthorizationPage} from '../accountDrawer/spotifyConnection/spotifyConnection';
import {AppLoader} from '../common/AppLoader';
import {
    contributorLabel,
    ContributorRow,
    CopyPhaseRow,
    formatCount,
    joinNames,
    PhaseState,
    PlaylistCover,
    PlaylistOptionRow,
    songs,
    SourceCard,
    WizardStep,
    WizardStepper,
} from './playlistCopyParts';
import './spotifyUtils.css';

const MAX_PLAYLIST_NAME_LENGTH = 100;
const INVALID_LINK_MESSAGE =
    "That doesn't look like a Spotify playlist link. Paste one like open.spotify.com/playlist/…, or pick a playlist from the list.";

type TargetMode = 'new' | 'existing';

type CopyState =
    | { status: 'idle' }
    | { status: 'running'; progress: PlaylistCopyProgress | null }
    | { status: 'done'; result: PlaylistCopyResult }
    | { status: 'stopped'; progress: PlaylistCopyProgress | null }
    | { status: 'failed'; message: string };

const COPY_HEADINGS: Record<CopyState['status'], string> = {
    idle: 'Copy',
    running: 'Copying songs…',
    done: 'Copy complete',
    stopped: 'Copy stopped',
    failed: "The copy didn't start",
};

interface CopyRun {
    sourceName: string;
    target: { kind: 'existing'; name: string } | { kind: 'new'; name: string };
}

const isAbortError = (error: unknown) => error instanceof DOMException && error.name === 'AbortError';

const filterPlaylists = createFilterOptions<SpotifyPlaylistSummary>({
    stringify: (playlist) => `${playlist.name} ${playlist.ownerName ?? ''}`,
});

const renderPlaylistOption = (
    props: React.HTMLAttributes<HTMLLIElement> & { key?: React.Key },
    playlist: SpotifyPlaylistSummary
) => {
    const {key, ...optionProps} = props;
    return (
        <li key={key} {...optionProps}>
            <PlaylistOptionRow playlist={playlist}/>
        </li>
    );
};

export const PlaylistContributorCopyTool: React.FC = () => {
    const [spotifyLinked, setSpotifyLinked] = useState<boolean | null>(null);
    const [needsReconnect, setNeedsReconnect] = useState(false);
    const [step, setStep] = useState<WizardStep>('source');

    const [myPlaylists, setMyPlaylists] = useState<SpotifyPlaylistSummary[] | null>(null);
    const [myPlaylistsError, setMyPlaylistsError] = useState<string | null>(null);

    const [sourceInput, setSourceInput] = useState('');
    const [pickedSource, setPickedSource] = useState<SpotifyPlaylistSummary | null>(null);
    const [sourceError, setSourceError] = useState<string | null>(null);
    const [previewLoading, setPreviewLoading] = useState(false);
    const [preview, setPreview] = useState<PlaylistContributorsPreview | null>(null);

    const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());

    const [targetMode, setTargetMode] = useState<TargetMode>('new');
    const [newPlaylistName, setNewPlaylistName] = useState('');
    const [targetPlaylistId, setTargetPlaylistId] = useState<string | null>(null);

    const [copyState, setCopyState] = useState<CopyState>({status: 'idle'});
    const [copyRun, setCopyRun] = useState<CopyRun | null>(null);

    const previewAbortRef = useRef<AbortController | null>(null);
    const copyAbortRef = useRef<AbortController | null>(null);
    const headingRef = useRef<HTMLHeadingElement>(null);
    const hasMountedRef = useRef(false);

    const handleApiError = useCallback((error: unknown): string | null => {
        if (error instanceof SpotifyUtilsApiError) {
            if (error.status === 403 || error.status === 410) {
                setNeedsReconnect(true);
                return null;
            }
            if (error.status === 422) {
                setSpotifyLinked(false);
                return null;
            }
            return error.message;
        }
        return 'Something went wrong. Check your connection and try again.';
    }, []);

    useEffect(() => {
        UseSpotifyConnected()
            .then(setSpotifyLinked)
            .catch(() => setSpotifyLinked(false));

        return () => {
            previewAbortRef.current?.abort();
            copyAbortRef.current?.abort();
        };
    }, []);

    const loadMyPlaylists = useCallback(async (signal?: AbortSignal) => {
        setMyPlaylistsError(null);
        try {
            setMyPlaylists(await fetchMyEditablePlaylists(signal));
        } catch (error) {
            if (isAbortError(error)) {
                return;
            }
            const message = handleApiError(error);
            if (message) {
                setMyPlaylistsError(message);
            }
        }
    }, [handleApiError]);

    useEffect(() => {
        if (!spotifyLinked || needsReconnect) {
            return;
        }
        const controller = new AbortController();
        void loadMyPlaylists(controller.signal);
        return () => controller.abort();
    }, [spotifyLinked, needsReconnect, loadMyPlaylists]);

    useEffect(() => {
        if (!hasMountedRef.current) {
            hasMountedRef.current = true;
            return;
        }
        headingRef.current?.focus();
    }, [step]);

    const sourceOptions = useMemo(
        () => [...(myPlaylists ?? [])].sort((a, b) => Number(b.collaborative) - Number(a.collaborative)),
        [myPlaylists]
    );

    const targetOptions = useMemo(
        () =>
            (myPlaylists ?? [])
                .filter((playlist) => playlist.id !== preview?.playlist.id)
                .sort((a, b) => Number(b.ownedByMe) - Number(a.ownedByMe)),
        [myPlaylists, preview]
    );

    const contributors = useMemo(() => preview?.contributors ?? [], [preview]);
    const selectedContributors = contributors.filter((contributor) => selectedIds.has(contributor.id));
    const selectedTrackCount = selectedContributors.reduce((sum, contributor) => sum + contributor.trackCount, 0);
    const allSelected = contributors.length > 0 && selectedContributors.length === contributors.length;

    const selectedTarget = targetOptions.find((playlist) => playlist.id === targetPlaylistId) ?? null;
    const trimmedName = newPlaylistName.trim();
    const targetReady = targetMode === 'new' ? trimmedName.length > 0 : selectedTarget !== null;
    const copyRunning = copyState.status === 'running';

    const loadPreview = useCallback(async (rawInput: string) => {
        const playlistId = parsePlaylistId(rawInput);
        if (!playlistId) {
            setSourceError(INVALID_LINK_MESSAGE);
            return;
        }

        if (preview?.playlist.id === playlistId) {
            setSourceError(null);
            setStep('contributors');
            return;
        }

        previewAbortRef.current?.abort();
        const controller = new AbortController();
        previewAbortRef.current = controller;
        setSourceError(null);
        setPreviewLoading(true);

        try {
            const result = await previewPlaylistContributors(playlistId, controller.signal);
            setPreview(result);
            setSelectedIds(new Set(result.contributors.length === 1 ? [result.contributors[0].id] : []));
            setNewPlaylistName(result.playlist.name.slice(0, MAX_PLAYLIST_NAME_LENGTH));
            setTargetPlaylistId((current) => (current === result.playlist.id ? null : current));
            setCopyState({status: 'idle'});
            setStep('contributors');
        } catch (error) {
            if (isAbortError(error)) {
                return;
            }
            const message = handleApiError(error);
            if (message) {
                setSourceError(message);
            }
        } finally {
            if (previewAbortRef.current === controller) {
                previewAbortRef.current = null;
                setPreviewLoading(false);
            }
        }
    }, [handleApiError, preview]);

    const submitSource = () => {
        const usePicked = pickedSource !== null && sourceInput === pickedSource.name;
        void loadPreview(usePicked ? pickedSource.id : sourceInput);
    };

    const toggleContributor = (id: string) => {
        setSelectedIds((current) => {
            const next = new Set(current);
            if (next.has(id)) {
                next.delete(id);
            } else {
                next.add(id);
            }
            return next;
        });
    };

    const toggleAll = () => {
        setSelectedIds(allSelected ? new Set() : new Set(contributors.map((contributor) => contributor.id)));
    };

    const startCopy = async () => {
        if (!preview || !targetReady || selectedContributors.length === 0 || copyRunning) {
            return;
        }

        copyAbortRef.current?.abort();
        const controller = new AbortController();
        copyAbortRef.current = controller;

        setCopyRun({
            sourceName: preview.playlist.name,
            target: targetMode === 'new'
                ? {kind: 'new', name: trimmedName}
                : {kind: 'existing', name: selectedTarget!.name},
        });
        setCopyState({status: 'running', progress: null});
        setStep('copy');

        let lastProgress: PlaylistCopyProgress | null = null;
        try {
            const result = await streamPlaylistCopy(
                {
                    sourcePlaylistId: preview.playlist.id,
                    contributorIds: selectedContributors.map((contributor) => contributor.id),
                    target: targetMode === 'new'
                        ? {kind: 'new', name: trimmedName}
                        : {kind: 'existing', playlistId: selectedTarget!.id},
                },
                (progress) => {
                    lastProgress = progress;
                    setCopyState({status: 'running', progress});
                },
                controller.signal
            );
            setCopyState({status: 'done', result});
            void loadMyPlaylists();
        } catch (error) {
            if (isAbortError(error)) {
                setCopyState({status: 'stopped', progress: lastProgress});
                void loadMyPlaylists();
                return;
            }
            const message = handleApiError(error);
            setCopyState(message ? {status: 'failed', message} : {status: 'idle'});
        } finally {
            if (copyAbortRef.current === controller) {
                copyAbortRef.current = null;
            }
        }
    };

    const stopCopy = () => copyAbortRef.current?.abort();

    const copyMoreFromSource = () => {
        const created = copyState.status === 'done' && copyState.result.createdTarget
            ? copyState.result.targetPlaylist
            : null;
        if (created) {
            setTargetMode('existing');
            setTargetPlaylistId(created.id);
        }
        setSelectedIds(new Set());
        setCopyState({status: 'idle'});
        setStep('contributors');
    };

    const startOver = () => {
        previewAbortRef.current?.abort();
        setPreview(null);
        setPickedSource(null);
        setSourceInput('');
        setSourceError(null);
        setSelectedIds(new Set());
        setTargetMode('new');
        setNewPlaylistName('');
        setTargetPlaylistId(null);
        setCopyState({status: 'idle'});
        setCopyRun(null);
        setStep('source');
    };

    const canOpenStep = (target: WizardStep) => {
        if (copyRunning) {
            return false;
        }
        switch (target) {
            case 'source':
                return true;
            case 'contributors':
                return preview !== null;
            case 'target':
                return preview !== null && selectedContributors.length > 0;
            default:
                return false;
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
            <section className="spotify-utils-tool">
                <h2 className="spotify-utils-tool__title">Copy songs by contributor</h2>
                <p className="spotify-utils-tool__hint">
                    {needsReconnect
                        ? 'This tool needs permission to read and edit your playlists. Reconnect Spotify once to grant it; you will come back here afterwards.'
                        : 'Link your Spotify account to copy songs between playlists.'}
                </p>
                <div className="spotify-utils-tool__actions">
                    <button
                        type="button"
                        className="spotify-utils-tool__primary"
                        onClick={redirectToSpotifyAuthorizationPage}
                    >
                        {needsReconnect ? 'Reconnect Spotify' : 'Connect Spotify'}
                    </button>
                </div>
            </section>
        );
    }

    const renderSourceStep = () => (
        <>
            <h3 className="playlist-copy__heading" ref={headingRef} tabIndex={-1}>
                Which playlist do you want to copy from?
            </h3>
            <form
                className="playlist-copy__form"
                onSubmit={(event) => {
                    event.preventDefault();
                    submitSource();
                }}
            >
                <Autocomplete<SpotifyPlaylistSummary, false, false, true>
                    freeSolo
                    fullWidth
                    options={sourceOptions}
                    loading={myPlaylists === null && !myPlaylistsError}
                    loadingText="Loading your playlists…"
                    groupBy={(playlist) => (playlist.collaborative ? 'Collaborative playlists' : 'Your playlists')}
                    getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
                    filterOptions={(options, state) => {
                        const pastedId = parsePlaylistId(state.inputValue);
                        return pastedId
                            ? options.filter((playlist) => playlist.id === pastedId)
                            : filterPlaylists(options, state);
                    }}
                    renderOption={renderPlaylistOption}
                    value={pickedSource}
                    inputValue={sourceInput}
                    onInputChange={(_, value) => {
                        setSourceInput(value);
                        setSourceError(null);
                    }}
                    onChange={(_, value) => {
                        if (value === null) {
                            setPickedSource(null);
                            return;
                        }
                        if (typeof value === 'string') {
                            void loadPreview(value);
                            return;
                        }
                        setPickedSource(value);
                        void loadPreview(value.id);
                    }}
                    disabled={previewLoading}
                    renderInput={(params) => (
                        <TextField
                            {...params}
                            label="Playlist link or name"
                            placeholder="Paste a Spotify link or search your playlists"
                            size="small"
                            error={sourceError !== null}
                            helperText={sourceError ?? 'Spotify only shares the songs of playlists you own or collaborate on.'}
                            onPaste={(event) => {
                                const pasted = event.clipboardData.getData('text');
                                if (parsePlaylistId(pasted)) {
                                    void loadPreview(pasted);
                                }
                            }}
                        />
                    )}
                />

                {previewLoading ? (
                    <div className="playlist-copy__status" role="status">
                        <AppLoader size={16}/>
                        Reading the playlist and counting who added what…
                    </div>
                ) : (
                    <div className="spotify-utils-tool__actions">
                        <button
                            type="submit"
                            className="spotify-utils-tool__primary"
                            disabled={!sourceInput.trim()}
                        >
                            Continue
                        </button>
                    </div>
                )}
            </form>

            {myPlaylistsError && (
                <p className="spotify-utils-tool__error">
                    Couldn't load your playlists: {myPlaylistsError}{' '}
                    <button type="button" className="spotify-utils-tool__link" onClick={() => void loadMyPlaylists()}>
                        Retry
                    </button>
                </p>
            )}

            <p className="playlist-copy__footnote">
                Playlists missing from the list?{' '}
                <button type="button" className="spotify-utils-tool__link" onClick={redirectToSpotifyAuthorizationPage}>
                    Reconnect Spotify
                </button>{' '}
                to grant playlist access.
            </p>
        </>
    );

    const renderContributorsStep = () => {
        if (!preview) {
            return null;
        }

        return (
            <>
                <SourceCard playlist={preview.playlist} onChange={() => setStep('source')}/>
                <div className="playlist-copy__heading-row">
                    <h3 className="playlist-copy__heading" ref={headingRef} tabIndex={-1}>
                        Whose songs should be copied?
                    </h3>
                    {contributors.length > 1 && (
                        <button type="button" className="spotify-utils-tool__link" onClick={toggleAll}>
                            {allSelected ? 'Clear' : 'Select all'}
                        </button>
                    )}
                </div>

                {contributors.length === 0 ? (
                    <p className="spotify-utils-tool__hint">
                        This playlist has no songs that can be copied. Local files, podcasts, and songs removed from
                        Spotify are left out.
                    </p>
                ) : (
                    <ul className="playlist-copy__contributors">
                        {contributors.map((contributor) => (
                            <ContributorRow
                                key={contributor.id || 'unknown'}
                                contributor={contributor}
                                totalTracks={preview.trackCount}
                                checked={selectedIds.has(contributor.id)}
                                onToggle={() => toggleContributor(contributor.id)}
                            />
                        ))}
                    </ul>
                )}

                {preview.skippedItemCount > 0 && (
                    <p className="playlist-copy__footnote">
                        {formatCount(preview.skippedItemCount)} local{' '}
                        {preview.skippedItemCount === 1 ? 'file, podcast, or unavailable song is' : 'files, podcasts, or unavailable songs are'}{' '}
                        left out.
                    </p>
                )}

                <div className="playlist-copy__footer">
                    <button type="button" className="spotify-utils-tool__secondary" onClick={() => setStep('source')}>
                        Back
                    </button>
                    <button
                        type="button"
                        className="spotify-utils-tool__primary"
                        disabled={selectedContributors.length === 0}
                        onClick={() => setStep('target')}
                    >
                        {selectedContributors.length === 0
                            ? 'Pick at least one person'
                            : `Continue with ${songs(selectedTrackCount)}`}
                    </button>
                </div>
            </>
        );
    };

    const renderTargetStep = () => {
        if (!preview) {
            return null;
        }

        const contributorNames = joinNames(selectedContributors.map(contributorLabel));

        return (
            <>
                <h3 className="playlist-copy__heading" ref={headingRef} tabIndex={-1}>
                    Where should the songs go?
                </h3>

                <div className="playlist-copy__segmented" role="radiogroup" aria-label="Target playlist">
                    {(['new', 'existing'] as const).map((mode) => (
                        <button
                            key={mode}
                            type="button"
                            role="radio"
                            aria-checked={targetMode === mode}
                            className={`playlist-copy__segment${targetMode === mode ? ' playlist-copy__segment--active' : ''}`}
                            onClick={() => setTargetMode(mode)}
                        >
                            {mode === 'new' ? 'New playlist' : 'Existing playlist'}
                        </button>
                    ))}
                </div>

                <form
                    className="playlist-copy__form"
                    onSubmit={(event) => {
                        event.preventDefault();
                        void startCopy();
                    }}
                >
                    {targetMode === 'new' ? (
                        <TextField
                            fullWidth
                            size="small"
                            label="Playlist name"
                            value={newPlaylistName}
                            onChange={(event) => setNewPlaylistName(event.target.value)}
                            error={!trimmedName}
                            helperText={
                                trimmedName
                                    ? `Private, on your account · ${newPlaylistName.length}/${MAX_PLAYLIST_NAME_LENGTH}`
                                    : 'Give the new playlist a name.'
                            }
                            slotProps={{htmlInput: {maxLength: MAX_PLAYLIST_NAME_LENGTH}}}
                        />
                    ) : myPlaylistsError ? (
                        <p className="spotify-utils-tool__error">
                            Couldn't load your playlists: {myPlaylistsError}{' '}
                            <button type="button" className="spotify-utils-tool__link" onClick={() => void loadMyPlaylists()}>
                                Retry
                            </button>
                        </p>
                    ) : myPlaylists !== null && targetOptions.length === 0 ? (
                        <p className="spotify-utils-tool__hint">
                            You don't have another playlist you can edit yet. Create a new one instead.
                        </p>
                    ) : (
                        <Autocomplete<SpotifyPlaylistSummary>
                            fullWidth
                            options={targetOptions}
                            loading={myPlaylists === null}
                            loadingText="Loading your playlists…"
                            groupBy={(playlist) => (playlist.ownedByMe ? 'Your playlists' : 'Shared with you')}
                            getOptionLabel={(playlist) => playlist.name}
                            filterOptions={filterPlaylists}
                            renderOption={renderPlaylistOption}
                            isOptionEqualToValue={(option, value) => option.id === value.id}
                            value={selectedTarget}
                            onChange={(_, value) => setTargetPlaylistId(value?.id ?? null)}
                            renderInput={(params) => (
                                <TextField
                                    {...params}
                                    size="small"
                                    label="Playlist"
                                    placeholder="Search playlists you can edit"
                                    helperText="Songs already in this playlist are skipped."
                                    InputProps={{
                                        ...params.InputProps,
                                        startAdornment: selectedTarget ? (
                                            <PlaylistCover imageUrl={selectedTarget.imageUrl} size={24}/>
                                        ) : params.InputProps.startAdornment,
                                    }}
                                />
                            )}
                        />
                    )}

                    {targetReady && (
                        <p className="playlist-copy__summary">
                            Copy <strong>{songs(selectedTrackCount)}</strong> added by <strong>{contributorNames}</strong>{' '}
                            {targetMode === 'new' ? (
                                <>into a new private playlist <strong>“{trimmedName}”</strong>.</>
                            ) : (
                                <>into <strong>“{selectedTarget!.name}”</strong>.</>
                            )}
                        </p>
                    )}

                    <div className="playlist-copy__footer">
                        <button
                            type="button"
                            className="spotify-utils-tool__secondary"
                            onClick={() => setStep('contributors')}
                        >
                            Back
                        </button>
                        <button type="submit" className="spotify-utils-tool__primary" disabled={!targetReady}>
                            Copy {songs(selectedTrackCount)}
                        </button>
                    </div>
                </form>
            </>
        );
    };

    const renderProgress = (progress: PlaylistCopyProgress | null, run: CopyRun) => {
        const phases: PlaylistCopyPhase[] = run.target.kind === 'existing'
            ? ['reading-source', 'reading-target', 'adding']
            : ['reading-source', 'adding'];
        const currentIndex = progress ? phases.indexOf(progress.phase) : 0;
        const stateOf = (index: number): PhaseState =>
            index < currentIndex ? 'done' : index === currentIndex ? 'active' : 'pending';

        const detailFor = (phase: PlaylistCopyPhase, state: PhaseState) => {
            if (!progress || state === 'pending') {
                return undefined;
            }
            if (phase === 'reading-source') {
                return state === 'done'
                    ? `${songs(progress.selectedTrackCount)} picked`
                    : `${formatCount(progress.processed)} of ${formatCount(progress.total)}`;
            }
            if (phase === 'reading-target') {
                return state === 'done'
                    ? 'Done'
                    : `${formatCount(progress.processed)} of ${formatCount(progress.total)}`;
            }
            return `${formatCount(progress.processed)} of ${formatCount(progress.total)}`;
        };

        const labelFor = (phase: PlaylistCopyPhase) => {
            switch (phase) {
                case 'reading-source':
                    return `Reading “${run.sourceName}”`;
                case 'reading-target':
                    return `Checking “${run.target.name}” for songs it already has`;
                case 'adding':
                    return run.target.kind === 'new'
                        ? `Creating “${run.target.name}” and adding songs`
                        : 'Adding songs';
            }
        };

        return (
            <ol className="playlist-copy__phases">
                {phases.map((phase, index) => {
                    const state = stateOf(index);
                    const isCurrent = progress?.phase === phase;
                    return (
                        <CopyPhaseRow
                            key={phase}
                            label={labelFor(phase)}
                            state={state}
                            detail={detailFor(phase, state)}
                            processed={isCurrent ? progress?.processed : undefined}
                            total={isCurrent ? progress?.total : undefined}
                        />
                    );
                })}
            </ol>
        );
    };

    const openInSpotifyLink = (playlist: SpotifyPlaylistSummary) => (
        <a className="spotify-utils-tool__primary playlist-copy__open" href={playlist.url} target="_blank" rel="noreferrer">
            Open in Spotify <OpenInNewRoundedIcon sx={{fontSize: 16}}/>
        </a>
    );

    const doneActions = (playlist: SpotifyPlaylistSummary | null) => (
        <div className="playlist-copy__footer playlist-copy__footer--start">
            {playlist && openInSpotifyLink(playlist)}
            <button type="button" className="spotify-utils-tool__secondary" onClick={copyMoreFromSource}>
                Copy more from this playlist
            </button>
            <button type="button" className="spotify-utils-tool__link" onClick={startOver}>
                Start over
            </button>
        </div>
    );

    const renderResult = (result: PlaylistCopyResult) => {
        const target = result.targetPlaylist;

        if (!target) {
            return (
                <div className="playlist-copy__result">
                    <p className="playlist-copy__result-title">Nothing to copy</p>
                    <p className="spotify-utils-tool__hint">
                        The selected people have no songs left in this playlist, so no playlist was created.
                    </p>
                    {doneActions(null)}
                </div>
            );
        }

        const partial = result.failureMessage !== null;
        const notAdded = result.selectedTrackCount - result.skippedDuplicateCount - result.addedCount;

        return (
            <div className={`playlist-copy__result${partial ? ' playlist-copy__result--warning' : ' playlist-copy__result--success'}`}>
                <div className="playlist-copy__result-header">
                    {partial
                        ? <WarningAmberRoundedIcon className="playlist-copy__result-icon"/>
                        : <CheckCircleRoundedIcon className="playlist-copy__result-icon"/>}
                    <div>
                        <p className="playlist-copy__result-title">
                            {partial
                                ? `Added ${songs(result.addedCount)} to “${target.name}”, then Spotify stopped`
                                : result.addedCount > 0
                                    ? `Added ${songs(result.addedCount)} to “${target.name}”`
                                    : `“${target.name}” already had every song`}
                        </p>
                        <p className="spotify-utils-tool__hint">
                            {partial
                                ? result.failureMessage
                                : result.createdTarget
                                    ? 'New private playlist on your account.'
                                    : 'Nothing was removed or reordered in the playlist.'}
                        </p>
                    </div>
                </div>

                <dl className="playlist-copy__stats">
                    <div>
                        <dt>Added</dt>
                        <dd>{formatCount(result.addedCount)}</dd>
                    </div>
                    <div>
                        <dt>Skipped as duplicates</dt>
                        <dd>{formatCount(result.skippedDuplicateCount)}</dd>
                    </div>
                    {partial && notAdded > 0 && (
                        <div>
                            <dt>Not added</dt>
                            <dd>{formatCount(notAdded)}</dd>
                        </div>
                    )}
                </dl>

                {doneActions(target)}
            </div>
        );
    };

    const renderCopyStep = () => {
        if (!copyRun) {
            return null;
        }

        return (
            <>
                <h3 className="playlist-copy__heading" ref={headingRef} tabIndex={-1}>
                    {COPY_HEADINGS[copyState.status]}
                </h3>

                <div aria-live="polite">
                    {copyState.status === 'running' && (
                        <>
                            {renderProgress(copyState.progress, copyRun)}
                            <div className="playlist-copy__footer playlist-copy__footer--start">
                                <button type="button" className="spotify-utils-tool__secondary" onClick={stopCopy}>
                                    Stop
                                </button>
                                <span className="playlist-copy__footnote">
                                    Stopping keeps the songs added so far.
                                </span>
                            </div>
                        </>
                    )}

                    {copyState.status === 'done' && renderResult(copyState.result)}

                    {copyState.status === 'stopped' && (
                        <div className="playlist-copy__result playlist-copy__result--warning">
                            <p className="spotify-utils-tool__hint">
                                {copyState.progress && copyState.progress.addedCount > 0
                                    ? `${songs(copyState.progress.addedCount)} were added before you stopped. Run the copy again to add the rest; songs already there are skipped.`
                                    : 'No songs were added.'}
                            </p>
                            <div className="playlist-copy__footer playlist-copy__footer--start">
                                {copyState.progress?.targetPlaylist && openInSpotifyLink(copyState.progress.targetPlaylist)}
                                <button type="button" className="spotify-utils-tool__secondary" onClick={copyMoreFromSource}>
                                    Back to contributors
                                </button>
                                <button type="button" className="spotify-utils-tool__link" onClick={startOver}>
                                    Start over
                                </button>
                            </div>
                        </div>
                    )}

                    {copyState.status === 'failed' && (
                        <div className="playlist-copy__result playlist-copy__result--error">
                            <p className="spotify-utils-tool__hint">{copyState.message}</p>
                            <div className="playlist-copy__footer playlist-copy__footer--start">
                                <button type="button" className="spotify-utils-tool__primary" onClick={() => void startCopy()}>
                                    Try again
                                </button>
                                <button type="button" className="spotify-utils-tool__secondary" onClick={() => setStep('target')}>
                                    Change target
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </>
        );
    };

    return (
        <section className="spotify-utils-tool playlist-copy">
            <div>
                <h2 className="spotify-utils-tool__title">Copy songs by contributor</h2>
                <p className="spotify-utils-tool__hint playlist-copy__intro">
                    Pick a shared playlist, choose whose songs to take, and copy them into a new or existing playlist.
                    Songs the target already has are skipped.
                </p>
            </div>

            <WizardStepper
                current={step}
                finished={step === 'copy' && copyState.status === 'done'}
                canOpen={canOpenStep}
                onOpen={setStep}
            />

            {step === 'source' && renderSourceStep()}
            {step === 'contributors' && renderContributorsStep()}
            {step === 'target' && renderTargetStep()}
            {step === 'copy' && renderCopyStep()}
        </section>
    );
};

export default PlaylistContributorCopyTool;
