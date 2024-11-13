import React, { FC, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import ProfileShow from "../components/profile/ProfileShow";
import { getCurrentUser } from "../components/common/frequentlyUsed";
import ListingsListPrivate from "../components/listing/listingOwner/listingsListPrivate";
import './styles/profilePage.css';

export const ProfilePageOwner = () => {
    const [username, setUsername] = useState<string>("");

    useEffect(() => {
        const fetchUser = async () => {
            const user = await getCurrentUser();
            if (user) {
                setUsername(user.name ?? "");
            }
        };

        fetchUser();
    }, []);

    return (
        <div id='main'>

            <ProfileShow username={username} isMyProfile={true}/>
            <div className={'profilePageListings'}>
                <ListingsListPrivate/>
            </div>

        </div>
    );
};