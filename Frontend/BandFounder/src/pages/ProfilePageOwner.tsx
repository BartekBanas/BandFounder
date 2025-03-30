import {useEffect, useState} from "react";
import ProfileShow from "../components/profile/ProfileShow";
import ListingsListPrivate from "../components/listing/listingOwner/listingsListPrivate";
import '../styles/customScrollbar.css'
import './styles/profilePage.css';
import {getAccount} from "../api/account";
import {getUserId} from "../hooks/authentication";

export const ProfilePageOwner = () => {
    const [username, setUsername] = useState<string>("");

    useEffect(() => {
        const fetchUser = async () => {
            const user = await getAccount(getUserId());
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