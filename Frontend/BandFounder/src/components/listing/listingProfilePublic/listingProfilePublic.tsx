import React, { useEffect, useState } from 'react';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './style.css';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {getUser} from "../../../api/account";
import OwnerListingElement from "./OwnerListingElement";
import {getListing} from "../../../api/listing";

interface listingProfilePublicProps {
    listingId: string;
}

export const ListingProfilePublic: React.FC<listingProfilePublicProps> = ({ listingId }) => {
    const [listing, setListing] = useState<any>(null);

    useEffect(() => {
        const fetchListing = async () => {
            const data = await getListing(listingId);
            if (data) {
                const owner = await getUser(data.ownerId);
                setListing({ ...data, owner });
            }
        };

        fetchListing();
    }, [listingId]);

    const theme = createTheme({
        components: {
            Loader: Loader.extend({
                defaultProps: {
                    loaders: { ...Loader.defaultLoaders, ring: RingLoader },
                    type: 'ring',
                },
            }),
        },
    });

    if (!listing) {
        return <div className="App-header">
            <MantineThemeProvider theme={theme}>
                <Loader size={200} />
            </MantineThemeProvider>
        </div>;
    }

    return (
        <div className={'listing'}>
            <div className={'listingHeader'}>
                    <OwnerListingElement listing={listing}/>
                <div className={'listingTitle'}>
                    <p>{listing?.name}</p>
                </div>
                <div className={'listingType'}>
                    <div className={`listingType-${listing.type}`}>
                        <p>{listing.type === 'CollaborativeSong' ? 'Song' : listing.type}</p>
                    </div>
                    <p>{listing?.genre}</p>
                </div>
            </div>
            <div className={'listingBody'}>
                <p>{listing?.description}</p>
            </div>
            <div className={'listingFooter'}>
                {listing?.musicianSlots.map((slot: any) => (
                    <div key={slot.id} className={`listingRole ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                        <img src={defaultProfileImage} alt="Default Profile"/>
                        <p>Role: {slot.role}</p>
                        <p>Status: {slot.status}</p>
                    </div>
                ))}
            </div>
        </div>
    );
};