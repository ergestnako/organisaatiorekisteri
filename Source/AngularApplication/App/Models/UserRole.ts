﻿"use strict";

module ServiceRegister
{
    export class UserRole implements Affecto.Base.IModel
    {
        constructor(public id: string, public name: string)
        {
        }
    }
}
 