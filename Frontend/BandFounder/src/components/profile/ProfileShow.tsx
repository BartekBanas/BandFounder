import React, { useEffect, useState } from 'react';
import { getProfile } from './api';

interface ProfileShowProps {
    username: string;
}



const ProfileShow: React.FC<ProfileShowProps> = ({ username }) => {
    return (
        <div>
            <h1>Profile</h1>
            <p>{username}</p>
        </div>
    );
};

export default ProfileShow;