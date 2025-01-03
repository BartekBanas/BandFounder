import {FC} from "react";
import {useRoutes} from "react-router-dom";
import {MainPage} from "./MainPage";
import {SpotifyConnectionPage} from "./SpotifyConnectionPage";
import {LoginPage} from "./LoginPage";
import {RegisterPage} from "./RegisterPage";
import {ProfilePage} from "./ProfilePage";
import {Main} from "./layout/Main";
import {useIsAuthenticated} from "../hooks/authentication";
import {ProfilePageOwner} from "./ProfilePageOwner";
import {MessagesPage} from "./MessagesPage";
import {RediractionPage} from "./RediractionPage";
import {MessagesPageMain} from "./MessagesPageMain";

const publicRoutes = [
    {
        path: "/",
        children: [
            {
                path: '/',
                element: <LoginPage/>
            },
            {
                path: '/register',
                element: <RegisterPage/>
            },
            {
                path: '*',
                element: <LoginPage/>
            }
        ]
    }
];

const privateRoutes = [{
    path: '/',
    element: <Main/>,
    children: [
        {
            path: '/',
            element: <MainPage/>
        },
        {
            path: '/home',
            element: <MainPage/>
        },
        {
            path: '/profile/:username',
            element: <ProfilePage/>
        },
        {
            path: '/profile',
            element: <ProfilePageOwner/>
        },
        {
            path: '/spotifyConnection/callback/',
            element: <SpotifyConnectionPage/>
        },
        {
            path: '/messages',
            element: <MessagesPageMain/>
        },
        {
            path: '/messages/:id',
            element: <MessagesPage/>
        },
        {
            path: '*',
            element: <RediractionPage/>
        },
    ]
}]

export const Routing: FC = function () {
    const isAuthenticated = useIsAuthenticated();
    let routes;

    if (isAuthenticated) {
        routes = privateRoutes;
    } else {
        routes = publicRoutes;
    }

    return useRoutes(routes);
};