import React, {FC, useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";
import './../assets/CustomScrollbar.css'
import ListingsListProfilePublic from "../components/listing/listingProfilePublic/listingsListProfilePublic";
import './styles/profilePage.css'
import {getAccount} from "../api/account";
import {getUserId} from "../hooks/authentication";

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    const {username} = useParams<{ username: string }>();
    const [isMyProfile, setIsMyProfile] = useState<boolean>(false);

    useEffect(() => {
        const checkProfileOwnership = async () => {
            const userId = getUserId();
            const account = await getAccount(userId);
            setIsMyProfile(account.name === username);
        };

        checkProfileOwnership();
    }, [username]);

    return (
        <div id='main' className={'custom-scrollbar'}>
            <ProfileShow username={username ?? "Unknown"} isMyProfile={isMyProfile}/>
            <div className={'profilePageListings'}>
                <ListingsListProfilePublic profileUsername={`${username}`}/>
            </div>
        </div>
    );
};
