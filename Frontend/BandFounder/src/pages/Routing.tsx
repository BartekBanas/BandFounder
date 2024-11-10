import {FC} from "react";
import {useRoutes} from "react-router-dom";
import {MainPage} from "./MainPage";
import {SpotifyConnectionPage} from "./SpotifyConnectionPage";
import {LoginPage} from "./LoginPage";
import {RegisterPage} from "./RegisterPage";
import {ProfilePage} from "./ProfilePage";
import {Main} from "./Main";
import {useIsAuthenticated} from "../hooks/authentication";

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
            path: '/spotifyConnection/callback/',
            element: <SpotifyConnectionPage/>
        }
    ]
}]

export const Routing: FC = function () {
    const isAuthorized = useIsAuthenticated();
    let routes;
    if (isAuthorized) {
        routes = privateRoutes;
    } else {
        routes = publicRoutes;
    }

    return useRoutes(routes);
};