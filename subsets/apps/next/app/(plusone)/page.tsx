'use client'

import React from 'react';
import { usePathname } from 'next/navigation';
import { GameComponent } from 'app/features'

const Page = () => {
    const currentPath = usePathname() || "";

    return <GameComponent path={currentPath} />;
};

export default Page;
