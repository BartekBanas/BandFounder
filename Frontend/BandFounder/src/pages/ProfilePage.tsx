import {FC, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";
import '../styles/customScrollbar.css'
import ListingsListProfilePublic from "../components/listing/listingProfilePublic/listingsListProfilePublic";
import './styles/profilePage.css'
import {getAccount, getAccountByUsername} from "../api/account";
import {getUserId} from "../hooks/authentication";
import {AppLoader} from "../components/common/AppLoader";
import {MissingContent} from "../components/common/MissingContent";

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    const {username} = useParams<{ username: string }>();
    const [isMyProfile, setIsMyProfile] = useState<boolean>(false);
    const [profileState, setProfileState] = useState<"loading" | "ready" | "missing" | "error">("loading");

    useEffect(() => {
        const checkProfileOwnership = async () => {
            setProfileState("loading");

            try {
                const [profile, currentAccount] = await Promise.all([
                    getAccountByUsername(username ?? ""),
                    getAccount(getUserId()),
                ]);
                setIsMyProfile(currentAccount.name === profile.name);
                setProfileState("ready");
            } catch (error) {
                if ((error as {status?: number}).status === 404) {
                    setProfileState("missing");
                    return;
                }

                console.error("Error loading profile:", error);
                setProfileState("error");
            }
        };

        checkProfileOwnership();
    }, [username]);

    if (profileState === "loading") {
        return <div className="App-header"><AppLoader size={200}/></div>;
    }

    if (profileState === "missing") {
        return (
            <MissingContent
                title="Profile not found"
                description="This profile may have been deleted or the link is incorrect."
                backTo="/home"
                backLabel="Go to home"
            />
        );
    }

    if (profileState === "error") {
        return (
            <MissingContent
                title="Profile unavailable"
                description="We couldn't load this profile right now. Please try again later."
                backTo="/home"
                backLabel="Go to home"
            />
        );
    }

    return (
        <div id='main' className={'custom-scrollbar'}>
            <ProfileShow username={username ?? "Unknown"} isMyProfile={isMyProfile}/>
            <div className={'profilePageListings'}>
                <ListingsListProfilePublic profileUsername={`${username}`}/>
            </div>
        </div>
    );
};
