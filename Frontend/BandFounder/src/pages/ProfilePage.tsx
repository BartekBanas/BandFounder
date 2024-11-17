import React, { FC } from "react";
import { useParams } from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";
import './../assets/CustomScrollbar.css'

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    const { username } = useParams<{ username: string }>();

    return (
        <div id='main' className={'custom-scrollbar'}>
            <ProfileShow username={username ?? ""} isMyProfile={false}/>
        </div>
    );
};
