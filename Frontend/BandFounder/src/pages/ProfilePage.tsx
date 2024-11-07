import React, { FC } from "react";
import { useParams } from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    const { username } = useParams<{ username: string }>();

    return (
        <div id='main'>
            <ProfileShow username={username ?? ""} />
        </div>
    );
};
