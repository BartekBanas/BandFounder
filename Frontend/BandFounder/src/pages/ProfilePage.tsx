import React, { FC } from "react";
import { useParams } from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";
import './../assets/CustomScrollbar.css'
import ListingsListProfilePublic from "../components/listing/listingProfilePublic/listingsListProfilePublic";
import './styles/profilePage.css'

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    const { username } = useParams<{ username: string }>();

    return (
        <div id='main' className={'custom-scrollbar'}>
            <ProfileShow username={username ?? ""} isMyProfile={false}/>
            <div className={'profilePageListings'}>
                <ListingsListProfilePublic profileUsername={`${username}`}/>
            </div>
        </div>
    );
};
