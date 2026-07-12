import React, {useState} from 'react';

import {Button} from "@mui/material";

import EditIcon from '@mui/icons-material/Edit';

import {updateListing} from "../../../api/listing";

import ProfilePicture from "../../profile/ProfilePicture";

import {DeleteListingButton} from "./DeleteListingButton";

import {Listing} from "../../../types/Listing";

import ListingCardView from "../shared/ListingCardView";

import ListingEditorModal from "../shared/ListingEditorModal";

import {useListingEditorState} from '../shared/useListingEditorState';



interface ListingPrivateProps {

    listing: Listing;

    onListingChanged: () => void | Promise<void>;

}



const toEditorValues = (listing: Listing) => ({
    name: listing.name,
    type: listing.type,
    genre: listing.genre,
    description: listing.description ?? '',
    musicianSlots: listing.musicianSlots,
});



const ListingPrivate: React.FC<ListingPrivateProps> = ({listing, onListingChanged}) => {

    const [open, setOpen] = useState(false);

    const editor = useListingEditorState(toEditorValues(listing));



    const handleOpen = () => {

        editor.reset(toEditorValues(listing));

        setOpen(true);

    };

    const handleClose = () => setOpen(false);



    const handleUpdateListing = async () => {

        try {

            await updateListing(editor.toUpdatePayload(), listing.id);

            handleClose();

            await onListingChanged();

        } catch (e) {

            console.error(e);

        }

    };



    return (

        <>

            <ListingCardView

                className="custom-scrollbar"

                listing={listing}

                ownerElement={

                    <div className="owner-listing-elements">

                        <ProfilePicture isMyProfile={true} accountId={listing.ownerId} size={40}/>

                    </div>

                }

                actions={

                    <>

                        <Button variant="contained" color="info" size="small" onClick={handleOpen}>

                            <span>Edit</span> <EditIcon/>

                        </Button>

                        <DeleteListingButton listingId={listing.id} onDeleted={onListingChanged}/>

                    </>

                }

            />



            <ListingEditorModal

                open={open}

                title="Edit Listing"

                submitLabel="Save"

                onClose={handleClose}

                onSubmit={handleUpdateListing}

                listingName={editor.listingName}

                onListingNameChange={editor.setListingName}

                listingType={editor.listingType}

                onListingTypeChange={editor.setListingType}

                listingGenre={editor.listingGenre}

                onListingGenreChange={editor.setListingGenre}

                listingDescription={editor.listingDescription}

                onListingDescriptionChange={editor.setListingDescription}

                musicianSlots={editor.listingMusicianSlots}

                onAddRole={editor.addSlot}

                onDeleteRole={editor.deleteSlot}

                onEditMusicianRole={editor.editSlotRole}

                onEditSlotStatus={editor.editSlotStatus}

                genres={editor.genres}

                roles={editor.roles}

            />

        </>

    );

};



export default ListingPrivate;


