import React, {useEffect, useState} from "react";
import {ActionIcon, Avatar, Button, CopyButton, Tooltip} from "@mantine/core";
import CheckRoundedIcon from "@mui/icons-material/CheckRounded";
import ChatBubbleOutlineRoundedIcon from "@mui/icons-material/ChatBubbleOutlineRounded";
import EditRoundedIcon from "@mui/icons-material/EditRounded";
import IosShareRoundedIcon from "@mui/icons-material/IosShareRounded";
import {getProfilePicture, uploadProfilePicture} from "../../api/account";
import {TopArtist} from "../../api/spotify";
import {Account} from "../../types/Account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import "./ProfileHeader.css";

const BANNER_MIN_CHARS = 90;

interface ProfileHeaderProps {
    account: Account;
    isMyProfile: boolean;
    genres: string[];
    topArtists: TopArtist[];
    onMessage: () => void;
    onEditProfile: () => void;
}

function initialsOf(name: string): string {
    const parts = name.split(/[\s._-]+/).filter(Boolean);
    const letters = parts.length > 1 ? parts[0][0] + parts[1][0] : name.slice(0, 2);
    return letters.toUpperCase();
}

function bannerLineOf(words: string[]): string {
    const phrase = words.slice(0, 6).join("  ·  ");
    return Array(Math.ceil(BANNER_MIN_CHARS / phrase.length) + 1).fill(phrase).join("  ·  ");
}

function useProfilePhoto(accountId: string): [string | null, () => void] {
    const [url, setUrl] = useState<string | null>(null);
    const [version, setVersion] = useState(0);

    useEffect(() => {
        let cancelled = false;
        getProfilePicture(accountId)
            .then((result) => !cancelled && setUrl(result))
            .catch(() => !cancelled && setUrl(null));
        return () => {
            cancelled = true;
        };
    }, [accountId, version]);

    return [url, () => setVersion((v) => v + 1)];
}

function profileUrlOf(username: string): string {
    return `${window.location.origin}/profile/${encodeURIComponent(username)}`;
}

interface AvatarUploadProps {
    onUploaded: () => void;
    children: React.ReactNode;
}

const AvatarUpload: React.FC<AvatarUploadProps> = ({onUploaded, children}) => {
    const handleChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) {
            return;
        }
        try {
            await uploadProfilePicture(file);
            mantineSuccessNotification("Profile picture updated");
            onUploaded();
        } catch {
            mantineErrorNotification("Failed to upload the profile picture.");
        }
    };

    return (
        <label className="profile-header__avatar-wrap profile-header__upload">
            <input type="file" accept="image/*" className="profile-header__upload-input" onChange={handleChange}/>
            {children}
            <span className="profile-header__upload-hint">Change photo</span>
        </label>
    );
};

const ProfileHeader: React.FC<ProfileHeaderProps> = ({account, isMyProfile, genres, topArtists, onMessage, onEditProfile}) => {
    const {name} = account;
    const [photo, refreshPhoto] = useProfilePhoto(account.id);
    const artistNames = topArtists.map((artist) => artist.name);
    const bannerLine = bannerLineOf(artistNames.length ? artistNames : genres.length ? genres : [name]);

    const avatar = (
        <Avatar unstyled className="profile-header__avatar" src={photo ?? undefined} alt={name}>
            <span className="profile-header__initials">{initialsOf(name)}</span>
        </Avatar>
    );

    return (
        <header className="profile-card profile-header">
            <div className="profile-header__banner" aria-hidden>
                <div className="profile-header__type">
                    <span>{bannerLine}</span>
                    <span>{bannerLine}</span>
                </div>
            </div>

            <div className="profile-header__body">
                {isMyProfile ? (
                    <AvatarUpload onUploaded={refreshPhoto}>{avatar}</AvatarUpload>
                ) : (
                    <div className="profile-header__avatar-wrap">{avatar}</div>
                )}

                <div className="profile-header__identity">
                    <div className="profile-header__title">
                        <h1 className="profile-header__name">{name}</h1>
                        {isMyProfile && <span className="profile-header__you">You</span>}
                    </div>
                    <span className="profile-header__handle">@{name}</span>
                </div>

                <div className="profile-header__actions">
                    {isMyProfile ? (
                        <Button
                            unstyled
                            className="profile-header__btn profile-header__btn--ghost"
                            onClick={onEditProfile}
                            leftSection={<EditRoundedIcon fontSize="inherit"/>}
                        >
                            Edit profile
                        </Button>
                    ) : (
                        <Button
                            unstyled
                            className="profile-header__btn profile-header__btn--primary"
                            onClick={onMessage}
                            leftSection={<ChatBubbleOutlineRoundedIcon fontSize="inherit"/>}
                        >
                            Message
                        </Button>
                    )}
                    <CopyButton value={profileUrlOf(name)} timeout={1500}>
                        {({copied, copy}) => (
                            <Tooltip label={copied ? "Copied" : "Copy profile link"} opened={copied || undefined} withArrow openDelay={300}>
                                <ActionIcon
                                    unstyled
                                    className="profile-header__icon-btn"
                                    aria-label={copied ? "Profile link copied" : "Share profile"}
                                    onClick={copy}
                                >
                                    {copied ? <CheckRoundedIcon fontSize="inherit"/> : <IosShareRoundedIcon fontSize="inherit"/>}
                                </ActionIcon>
                            </Tooltip>
                        )}
                    </CopyButton>
                </div>
            </div>
        </header>
    );
};

export default ProfileHeader;
