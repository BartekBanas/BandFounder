// src/pages/ProfilePage.tsx
import React, { FC } from "react";
import ProfileShow from "../components/profile/ProfileShow";

interface ProfilePageProps {}

export const ProfilePage: FC<ProfilePageProps> = () => {
    return (
        <div id='main'>
            <ProfileShow />
        </div>
    );
};