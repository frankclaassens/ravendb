﻿
import appUrl = require("common/appUrl");
import separatorMenuItem = require("common/shell/menu/separatorMenuItem");
import intermediateMenuItem = require("common/shell/menu/intermediateMenuItem");
import leafMenuItem = require("common/shell/menu/leafMenuItem");
import resource = require("models/resources/resource");

import getManageServerMenuItem = require("common/shell/menu/items/manageServer");
import getResourcesMenuItem = require("common/shell/menu/items/resources");
import getStatsMenuItem = require("common/shell/menu/items/stats");
import getSettingsMenuItem = require("common/shell/menu/items/settings");
import getTasksMenuItem = require("common/shell/menu/items/tasks");
import getQueryMenuItem = require("common/shell/menu/items/query");
import getIndexesMenuItem = require("common/shell/menu/items/indexes");
import getDocumentsMenuItem = require("common/shell/menu/items/documents");

export = generateMenuItems;

function generateMenuItems(resource: resource) {
    if (!resource) {
        return generateNoActiveResourceMenuItems();
    } else if (resource.isDatabase()) {
        return generateActiveDatabaseMenuItems();
    } else {
        throw new Error(`Menu items for resource of type ${ resource.fullTypeName } are not implemented.`);
    }
}

function generateNoActiveResourceMenuItems() {
    let appUrls = appUrl.forCurrentDatabase();
    return [
        new separatorMenuItem('Manage'),
        getTasksMenuItem(appUrls),
        getSettingsMenuItem(appUrls),
        getStatsMenuItem(appUrls),
        new separatorMenuItem('Server'),
        getResourcesMenuItem(appUrls),
        getManageServerMenuItem(),
        new leafMenuItem({
            route: '',
            moduleId: '',
            title: 'About',
            tooltip: "About",
            nav: true,
            css: 'fa fa-question-mark',
            dynamicHash: ko.computed(() => 'TODO')
        })
    ];
    
}


function generateActiveDatabaseMenuItems() {
    let appUrls = appUrl.forCurrentDatabase();
    return [
        getDocumentsMenuItem(appUrls),
        getIndexesMenuItem(appUrls),
        getQueryMenuItem(appUrls),
        new separatorMenuItem('Manage'),
        getTasksMenuItem(appUrls),
        getSettingsMenuItem(appUrls),
        getStatsMenuItem(appUrls),
        new separatorMenuItem('Server'),
        getResourcesMenuItem(appUrls),
        getManageServerMenuItem(),
        new leafMenuItem({
            route: '',
            moduleId: '',
            title: 'About',
            tooltip: "About",
            nav: true,
            css: 'fa fa-question-mark',
            dynamicHash: ko.computed(() => 'TODO')
        })
    ];
}


