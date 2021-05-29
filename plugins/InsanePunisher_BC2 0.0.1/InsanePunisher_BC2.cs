/*
 * Copyright 2010 Miguel Mendoza - miguel@micovery.com
 *
 * Insane Punisher is free software: you can redistribute it and/or modify it under the terms of the 
 * GNU General Public License as published by the Free Software Foundation, either version 3 of the License, 
 * or (at your option) any later version. Insane Punisher is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details. You should have received a copy of the 
 * GNU General Public License along with Insane Punisher. If not, see http://www.gnu.org/licenses/.
 * 
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{

    public class InsanePunisher_BC2 : PRoConPluginAPI, IPRoConPluginInterface
    {

        string settings = @"
        ######################################################################################
        ##   Note To Editors 
        ######################################################################################
        ####
        ####   This is what you need to know:
        ####
        ####         DO NOT USE THE DOUBLE-QUOTE CHARACTER INSIDE THIS STRING
        ####         IF YOU HAVE TO USE IT, THEN USE A DOUBLE-DOUBLE-QUOTE.
        ####         THAT IS TWO CONSECUTIVE DOUBLE-QUOTE CHARACTERS TOGETHER LIKE THIS: 
        ####             
        ####                                     ""
        ####
        ####     
         

        ######################################################################################
        ##   Default Zone Definitions 
        ######################################################################################
        ###
        ###  For conquest mode, both US, and RU bases, are defined. For rush mode, only the 
        ###  base of the attacker team is defined. (except in Whitepass where the attackers have a defense zone as well)
        ###  
        ###

        #Conquest - Panama Canal
        procon.protected.zones.add ""9e35828d-b740-469b-970f-6910f7cde55e"" ""levels/mp_001"" ""RU_BASE"" 7 146 293 0 93 239 0 80 249 0 76 266 0 60 280 0 81 300 0 111 328 0
        procon.protected.zones.add ""8b8a9519-370e-42da-879b-57577d4af541"" ""levels/mp_001"" ""US_BASE"" 4 -239 -240 0 -173 -173 0 -139 -208 0 -203 -272 0

        #Conquest - Laguna Alta
        procon.protected.zones.add ""bGV2ZWxzL21wXzAwMzE1NTg2MTU2MTY="" ""levels/mp_003"" ""US_BASE"" 14 53 -141 0 67 -148 0 78 -158 0 84 -169 0 87 -182 0 85 -193 0 82 -201 0 104 -191 0 109 -175 0 106 -161 0 100 -150 0 90 -143 0 72 -142 0 59 -131 0
        procon.protected.zones.add ""cbb81ad3-657a-43fd-9089-9e7dcc0e0446"" ""levels/mp_003"" ""RU_BASE"" 15 86 157 0 98 154 0 102 144 0 102 134 0 103 127 0 104 120 0 103 114 0 102 106 0 68 86 0 60 96 0 58 106 0 57 119 0 67 136 0 67 148 0 74 155 0


        #Conquest - Atacama Desert
        procon.protected.zones.add ""14884e91-9b8c-43af-9a52-06564c132f7c"" ""levels/mp_005"" ""RU_BASE"" 12 -464 -93 0 -462 -60 0 -428 -34 0 -435 -20 0 -403 -2 0 -327 -100 0 -346 -115 0 -309 -163 0 -355 -197 0 -341 -217 0 -373 -237 0 -465 -128 0
        procon.protected.zones.add ""e6a848a6-b6c8-43ce-9e14-d0f46530443c"" ""levels/mp_005"" ""RU_DEFENSE"" 4 -446 -89 0 -430 -78 0 -442 -64 0 -459 -75 0
        procon.protected.zones.add ""deaa8d63-5eba-458d-a906-03234d25803b"" ""levels/mp_005"" ""RU_DEFENSE"" 4 -381 -37 0 -394 -46 0 -371 -74 0 -363 -65 0
        procon.protected.zones.add ""b49ec761-bb9c-44db-b959-8046cd7f3ca1"" ""levels/mp_005"" ""RU_DEFENSE"" 4 -433 -137 0 -420 -130 0 -430 -116 0 -444 -125 0
        procon.protected.zones.add ""427f0ecc-e4a8-4522-8656-79687d1a0607"" ""levels/mp_005"" ""US_BASE"" 7 41 422 0 54 401 0 77 371 0 95 380 0 154 303 0 298 406 0 196 529 0
        procon.protected.zones.add ""b076d657-9f72-4b0a-811c-20f2b525f795"" ""levels/mp_005"" ""US_DEFENSE"" 4 232 488 0 213 473 0 226 459 0 244 472 0
        procon.protected.zones.add ""7bec0c16-b62d-4155-826b-a5c03ffe7c17"" ""levels/mp_005"" ""US_DEFENSE"" 4 227 448 0 248 463 0 264 440 0 244 426 0
        procon.protected.zones.add ""995051eb-86ca-400a-a69d-e56ec96820fb"" ""levels/mp_005"" ""US_DEFENSE"" 4 85 387 0 73 401 0 91 416 0 104 398 0


        #Conquest - Arica Harbor
        procon.protected.zones.add ""ec55727a-9fa7-41fc-8ac7-278a2011dea4"" ""levels/mp_006cq"" ""US_BASE"" 6 254 302 0 277 236 0 271 208 0 306 166 0 464 243 0 410 356 0
        procon.protected.zones.add ""391c01b9-1250-4ae4-b7b0-dc7297d93818"" ""levels/mp_006cq"" ""US_DEFENSE"" 4 286 278 0 278 300 0 259 291 0 267 271 0
        procon.protected.zones.add ""50b2501d-7cf4-454d-b837-d0741071b4ae"" ""levels/mp_006cq"" ""US_DEFENSE"" 4 290 267 0 268 261 0 276 236 0 298 243 0
        procon.protected.zones.add ""3438c52c-2a51-4e7b-9120-c70ee086ef3c"" ""levels/mp_006cq"" ""RU_BASE"" 4 1 13 0 81 -68 0 114 -39 0 20 50 0


        #Conquest - White Pass
        procon.protected.zones.add ""f329e633-cb83-45cc-91ec-f7f60553751d"" ""levels/mp_007"" ""RU_BASE"" 8 -163 -99 0 -113 -123 0 -111 -138 0 -119 -153 0 -138 -151 0 -150 -144 0 -161 -135 0 -166 -118 0
        procon.protected.zones.add ""a98a3c06-d18f-42cb-a42b-20e6723c08fa"" ""levels/mp_007"" ""RU_DEFENSE"" 4 -119 -132 0 -127 -132 0 -127 -121 0 -119 -121 0
        procon.protected.zones.add ""7960178e-53b9-4751-8938-a145c306c9db"" ""levels/mp_007"" ""US_BASE"" 4 189 -208 0 189 -150 0 224 -150 0 224 -209 0
        procon.protected.zones.add ""d2ea19d7-faa8-4def-ac93-ec4eb4b11b89"" ""levels/mp_007"" ""US_DEFENSE"" 4 207 -168 0 206 -157 0 216 -157 0 216 -168 0


        #Conquest - Nelson Bay
        procon.protected.zones.add ""a4ea2343-d466-49b0-9f7d-8c404ca47e0e"" ""levels/mp_008cq"" ""US_BASE"" 4 306 -211 0 269 -248 0 204 -188 0 242 -151 0
        procon.protected.zones.add ""25772017-bfcd-4aca-8238-4bab1c8c96f9"" ""levels/mp_008cq"" ""RU_BASE"" 9 -203 -119 0 -181 -119 0 -153 -140 0 -149 -152 0 -149 -163 0 -158 -169 0 -169 -170 0 -184 -164 0 -204 -162 0


        #Conquest - Laguna Presa
        procon.protected.zones.add ""a6c91076-7e95-4d0b-99a3-22babd2574f0"" ""levels/mp_009cq"" ""US_BASE"" 12 53 -1 0 53 29 0 8 72 0 1 62 0 -9 53 0 -20 46 0 -33 47 0 -41 56 0 -58 45 0 -37 20 0 5 5 0 37 4 0
        procon.protected.zones.add ""d291a6dd-fbc6-475f-89aa-3f40ddb5a633"" ""levels/mp_009cq"" ""US_DEFENSE"" 4 18 67 0 9 56 0 -3 66 0 7 77 0
        procon.protected.zones.add ""52c00a92-1aee-493b-ba9a-1b9536c01bdb"" ""levels/mp_009cq"" ""US_DEFENSE"" 4 47 17 0 59 17 0 59 5 0 46 5 0
        procon.protected.zones.add ""c7e8506a-6600-40b4-8987-e7634fd5a459"" ""levels/mp_009cq"" ""RU_BASE"" 6 222 225 0 148 301 0 169 326 0 209 301 0 237 252 0 232 238 0
        procon.protected.zones.add ""3dd5a6db-90d2-4e52-916e-ff6d9e49a2fb"" ""levels/mp_009cq"" ""RU_DEFENSE"" 4 211 296 0 219 285 0 200 272 0 192 283 0
        procon.protected.zones.add ""33b1d6c7-1ba0-4571-bc0e-0ff45e73baf6"" ""levels/mp_009cq"" ""RU_DEFENSE"" 4 182 280 0 195 297 0 184 304 0 171 288 0


        #Conquest - Port Valdez
        procon.protected.zones.add ""dfbcb6ef-ee6c-4e9e-9b40-6ff8d08af8ec"" ""levels/mp_012cq"" ""RU_BASE"" 13 8 466 0 48 466 0 49 428 0 67 404 0 64 387 0 50 377 0 48 361 0 27 343 0 -19 390 0 -7 404 0 11 414 0 5 434 0 4 449 0
        procon.protected.zones.add ""72510dc2-a73e-48ac-b9ba-4652c590458a"" ""levels/mp_012cq"" ""RU_DEFENSE"" 4 35 381 0 26 389 0 35 398 0 44 390 0
        procon.protected.zones.add ""aaf048c1-7146-4b14-b592-1652a36fab75"" ""levels/mp_012cq"" ""RU_DEFENSE"" 4 -2 384 0 -10 392 0 -3 400 0 5 392 0
        procon.protected.zones.add ""874a3067-214f-4ad0-b7a6-339c0719f3a1"" ""levels/mp_012cq"" ""US_BASE"" 13 250 -291 0 245 -307 0 191 -348 0 177 -330 0 165 -338 0 144 -309 0 114 -310 0 110 -273 0 153 -269 0 152 -242 0 169 -230 0 211 -241 0 232 -261 0
        procon.protected.zones.add ""b6f76ca9-c572-4553-a475-38f98107edaa"" ""levels/mp_012cq"" ""US_DEFENSE"" 4 204 -311 0 217 -304 0 225 -314 0 214 -321 0
        procon.protected.zones.add ""6e97519f-b12b-4a2b-b4e8-977e88aa7e15"" ""levels/mp_012cq"" ""US_DEFENSE"" 4 167 -305 0 175 -314 0 186 -307 0 179 -296 0
        procon.protected.zones.add ""b03c5604-091f-4a88-8bde-90dd77a529bb"" ""levels/mp_012cq"" ""US_DEFENSE"" 4 195 -295 0 181 -279 0 190 -272 0 203 -289 0
        procon.protected.zones.add ""6b02e88c-1708-48af-8b86-667f15f4d538"" ""levels/mp_012cq"" ""RU_DEFENSE"" 4 28 374 0 38 365 0 30 356 0 19 366 0


        #Conquest - Oasis
        procon.protected.zones.add ""93c0d084-5c0b-4cbe-8943-503f628ca930"" ""levels/bc1_oasis_cq"" ""US_BASE"" 12 495 186 0 494 196 0 483 200 0 463 197 0 436 200 0 419 199 0 385 200 0 387 165 0 399 149 0 428 141 0 446 145 0 491 168 0
        procon.protected.zones.add ""f2593082-43d3-4295-9166-e43b38253428"" ""levels/bc1_oasis_cq"" ""RU_BASE"" 11 374 505 0 384 506 0 389 515 0 385 529 0 443 529 0 452 555 0 438 591 0 396 592 0 350 570 0 343 536 0 365 513 0


        #Conquest - Harvest Day
        procon.protected.zones.add ""f897db72-63c2-41eb-ac0b-028df09b3235"" ""levels/bc1_harvest_day_cq"" ""RU_BASE"" 4 58 868 0 66 790 0 164 793 0 155 866 0
        procon.protected.zones.add ""01a5c331-9016-44a6-870e-6195d4908476"" ""levels/bc1_harvest_day_cq"" ""RU_DEFENSE"" 4 130 811 0 132 799 0 118 798 0 117 811 0
        procon.protected.zones.add ""c3e2087c-ef19-42d6-a1f3-780f8deef83d"" ""levels/bc1_harvest_day_cq"" ""RU_DEFENSE"" 4 95 809 0 97 797 0 108 798 0 107 810 0
        procon.protected.zones.add ""6146f806-ef48-447e-a87e-b12346a99f31"" ""levels/bc1_harvest_day_cq"" ""US_BASE"" 6 576 38 0 500 44 0 494 -12 0 519 -45 0 556 -52 0 584 -34 0
        procon.protected.zones.add ""a1893630-4e1f-4be0-8146-122b49175182"" ""levels/bc1_harvest_day_cq"" ""US_DEFENSE"" 4 502 -5 0 501 8 0 515 8 0 516 -5 0
        procon.protected.zones.add ""a1893630-4e1f-4be0-8146-122b49175183"" ""levels/bc1_harvest_day_cq"" ""US_DEFENSE"" 4 522 -28 0 525 -41 0 537 -40 0 535 -27 0


        #Conquest - Heavy Metal
        procon.protected.zones.add ""7760073d-626b-40d7-ac3f-bde0461adce2"" ""levels/mp_sp_005cq"" ""RU_BASE"" 6 -329 691 0 -498 657 0 -491 560 0 -460 539 0 -293 567 0 -283 624 0
        procon.protected.zones.add ""56a9363b-4612-4b16-80e4-3bdca356e6c7"" ""levels/mp_sp_005cq"" ""US_BASE"" 10 -256 -746 0 -273 -724 0 -265 -658 0 -258 -583 0 -192 -581 0 -140 -618 0 -143 -641 0 -143 -661 0 -155 -740 0 -229 -750 0
        procon.protected.zones.add ""70800640-15e6-477a-9e89-de76f860e853"" ""levels/mp_sp_005cq"" ""US_DEFENSE"" 4 -199 -665 0 -190 -652 0 -175 -661 0 -183 -674 0
        procon.protected.zones.add ""88d7e871-d69c-4c57-ac2c-170989b95069"" ""levels/mp_sp_005cq"" ""RU_DEFENSE"" 4 -421 638 0 -422 625 0 -407 625 0 -408 637 0
        procon.protected.zones.add ""cfd837e6-57d4-4830-a9b3-0437b5d5af6a"" ""levels/mp_sp_005cq"" ""RU_DEFENSE"" 4 -420 615 0 -421 603 0 -408 602 0 -407 616 0
        procon.protected.zones.add ""f78e8f24-709a-46f0-bca6-7f0d31b3a100"" ""levels/mp_sp_005cq"" ""RU_DEFENSE"" 4 -397 620 0 -398 635 0 -383 635 0 -382 621 0
        procon.protected.zones.add ""34c00fce-a885-4e9a-88b7-80130b457443"" ""levels/mp_sp_005cq"" ""US_DEFENSE"" 4 -196 -641 0 -195 -627 0 -212 -625 0 -212 -641 0
        procon.protected.zones.add ""9bebe1ce-1227-422e-9fb5-89e6e8a2362e"" ""levels/mp_sp_005cq"" ""US_DEFENSE"" 4 -214 -661 0 -228 -651 0 -238 -667 0 -224 -677 0


        #Conquest - Phu Bai Valley
        procon.protected.zones.add ""c4cb6fa6-943a-40ae-8103-fa7616153a97"" ""levels/nam_mp_006cq"" ""US_BASE"" 21 141 280 0 166 276 0 175 239 0 169 195 0 160 182 0 146 178 0 131 176 0 116 179 0 97 180 0 85 184 0 66 194 0 63 189 0 56 182 0 47 182 0 45 190 0 50 199 0 57 211 0 62 229 0 65 249 0 86 274 0 115 280 0
        procon.protected.zones.add ""6830ad3d-4e38-475f-b935-47df13c16b6c"" ""levels/nam_mp_006cq"" ""US_DEFENSE"" 4 127 181 0 136 181 0 136 190 0 127 190 0
        procon.protected.zones.add ""9b6fd076-9b10-4996-bcbb-325e80f86ede"" ""levels/nam_mp_006cq"" ""US_DEFENSE"" 4 92 188 0 87 194 0 82 188 0 87 183 0
        procon.protected.zones.add ""5713a3f5-21c5-43ca-8281-c17308a81c4a"" ""levels/nam_mp_006cq"" ""US_DEFENSE"" 4 59 189 0 53 194 0 48 187 0 55 183 0
        procon.protected.zones.add ""7b8eee46-0967-42e2-8b5b-a9e6fea7da4c"" ""levels/nam_mp_006cq"" ""NVA_BASE"" 16 -162 -213 0 -174 -199 0 -175 -189 0 -143 -172 0 -122 -162 0 -99 -157 0 -69 -170 0 -62 -174 0 -58 -179 0 -38 -197 0 -50 -219 0 -55 -230 0 -69 -228 0 -102 -237 0 -116 -250 0 -139 -243 0

        #Conquest - Vantage Point
        procon.protected.zones.add ""9dbfacbd-e0c8-435d-a09a-162caba088ec"" ""levels/nam_mp_002cq"" ""NVA_BASE"" 15 -182 218 0 -169 214 0 -157 200 0 -155 185 0 -174 170 0 -185 164 0 -194 157 0 -204 153 0 -215 153 0 -220 164 0 -216 177 0 -208 188 0 -200 198 0 -191 208 0 -188 215 0
        procon.protected.zones.add ""3f38b405-ba54-44b4-985b-2bf449427be2"" ""levels/nam_mp_002cq"" ""US_BASE"" 12 -119 -257 0 -128 -253 0 -133 -242 0 -130 -233 0 -125 -226 0 -111 -217 0 -98 -218 0 -86 -229 0 -82 -237 0 -87 -244 0 -94 -243 0 -110 -256 0

        #Conquest - Cao Son Temple
        procon.protected.zones.add ""8b4004c0-8107-4a25-9fcc-e9ba65104c52"" ""levels/nam_mp_005cq"" ""NVA_BASE"" 19 190 -22 0 188 -41 0 179 -59 0 179 -75 0 180 -86 0 184 -94 0 192 -101 0 199 -103 0 213 -102 0 224 -97 0 234 -78 0 234 -79 0 233 -68 0 229 -46 0 222 -39 0 214 -32 0 208 -20 0 201 -15 0 190 -6 0
        procon.protected.zones.add ""5414fd38-fa16-4d23-b076-9890c942c155"" ""levels/nam_mp_005cq"" ""US_BASE"" 25 74 316 0 63 303 0 43 303 0 43 277 0 43 254 0 55 246 0 76 245 0 89 232 0 100 231 0 111 231 0 119 231 0 130 234 0 139 241 0 144 249 0 147 262 0 144 276 0 138 286 0 134 299 0 137 313 0 134 325 0 124 333 0 111 335 0 98 332 0 87 325 0 83 321 0

        #Conquest - Hill 137
        procon.protected.zones.add ""eea82261-67f1-4ed8-8bd0-672486af275b"" ""levels/nam_mp_003cq"" ""NVA_BASE"" 13 160 347 0 171 338 0 174 318 0 160 302 0 162 271 0 158 262 0 146 259 0 114 274 0 105 284 0 104 297 0 122 315 0 141 330 0 150 349 0
        procon.protected.zones.add ""558587c6-e904-4e7b-a521-d173a0e58bac"" ""levels/nam_mp_003cq"" ""US_BASE"" 19 103 -26 0 113 -31 0 124 -31 0 162 -21 0 176 -23 0 187 -37 0 204 -68 0 198 -83 0 190 -93 0 181 -94 0 165 -100 0 142 -110 0 104 -111 0 86 -99 0 86 -84 0 79 -54 0 82 -42 0 89 -34 0 94 -28 0

        #Conquest - Operation Hastings
        procon.protected.zones.add ""5b02d3be-0924-4997-a613-fea335b637eb"" ""levels/nam_mp_007cq"" ""NVA_BASE"" 12 23 171 0 37 168 0 55 170 0 70 175 0 82 180 0 82 239 0 16 238 0 4 224 0 -2 212 0 -3 196 0 2 186 0 11 179 0
        procon.protected.zones.add ""27e6ccbb-b7fd-4df3-9a12-583f7d9be3b9"" ""levels/nam_mp_007cq"" ""US_BASE"" 17 -132 -203 0 -146 -203 0 -160 -209 0 -172 -217 0 -182 -224 0 -190 -234 0 -194 -244 0 -195 -256 0 -194 -268 0 -189 -281 0 -114 -282 0 -110 -269 0 -106 -255 0 -105 -241 0 -104 -226 0 -108 -215 0 -118 -206 0


        #Rush - Valparaiso
        procon.protected.zones.add ""da140614-72d5-4c89-9926-720319440b06gr"" ""levels/mp_002"" ""US_BASE"" 13 -114 299 0 -110 319 0 -95 332 0 -76 341 0 -56 341 0 -32 345 0 -8 347 0 0 270 0 -28 263 0 -51 255 0 -75 255 0 -95 262 0 -111 277 0

        #Rush - Isla Inocentes
        procon.protected.zones.add ""eb0b9f8d-4b31-48aa-8f62-49553b05fce3gr"" ""levels/mp_004"" ""US_BASE"" 17 -126 -416 0 -160 -403 0 -181 -366 0 -175 -287 0 -167 -260 0 -152 -248 0 -141 -243 0 -128 -247 0 -118 -254 0 -109 -265 0 -104 -277 0 -112 -296 0 -85 -342 0 -75 -355 0 -71 -370 0 -75 -385 0 -89 -401 0

        #Rush - Arica Harbor
        procon.protected.zones.add ""1edd3a6d-33f5-4113-a18c-238052ce40b1gr"" ""levels/mp_006"" ""US_BASE"" 15 748 -93 0 670 -43 0 658 -28 0 650 -11 0 652 6 0 659 23 0 689 60 0 705 65 0 722 59 0 742 47 0 820 -7 0 826 -20 0 824 -33 0 785 -94 0 767 -98 0

        #Rush - White Pass
        procon.protected.zones.add ""7960178e-53b9-4751-8938-a145c306c9dbgr"" ""levels/mp_007gr"" ""US_BASE"" 4 189 -208 0 189 -150 0 224 -150 0 224 -209 0
        procon.protected.zones.add ""d2ea19d7-faa8-4def-ac93-ec4eb4b11b89gr"" ""levels/mp_007gr"" ""US_DEFENSE"" 4 207 -168 0 206 -157 0 216 -157 0 216 -168 0

        #Rush - Nelson Bay
        procon.protected.zones.add ""25772017-bfcd-4aca-8238-4bab1c8c96f9gr"" ""levels/mp_008"" ""RU_BASE"" 9 -203 -119 0 -181 -119 0 -153 -140 0 -149 -152 0 -149 -163 0 -158 -169 0 -169 -170 0 -184 -164 0 -204 -162 0

        #Rush - Laguna Presa
        procon.protected.zones.add ""2fda5fb7-ce5a-4944-ae20-42dc379344acgr"" ""levels/mp_009gr"" ""US_BASE"" 16 -282 -336 0 -297 -334 0 -310 -322 0 -352 -294 0 -325 -252 0 -307 -233 0 -293 -229 0 -283 -237 0 -283 -250 0 -294 -262 0 -298 -273 0 -293 -285 0 -281 -296 0 -267 -309 0 -263 -321 0 -269 -334 0

        #Rush - Port Valdez
        procon.protected.zones.add ""dfbcb6ef-ee6c-4e9e-9b40-6ff8d08af8ecgr"" ""levels/mp_012gr"" ""RU_BASE"" 13 8 466 0 48 466 0 49 428 0 67 404 0 64 387 0 50 377 0 48 361 0 27 343 0 -19 390 0 -7 404 0 11 414 0 5 434 0 4 449 0

        #Rush - Atacama Desert
        procon.protected.zones.add ""14884e91-9b8c-43af-9a52-06564c132f7cgr"" ""levels/mp_005gr"" ""US_BASE"" 12 -464 -93 0 -462 -60 0 -428 -34 0 -435 -20 0 -403 -2 0 -327 -100 0 -346 -115 0 -309 -163 0 -355 -197 0 -341 -217 0 -373 -237 0 -465 -128 0

        #Rush - Harvest Day
        procon.protected.zones.add ""f897db72-63c2-41eb-ac0b-028df09b3235gr"" ""levels/bc1_harvest_day_gr"" ""US_BASE"" 4 58 868 0 66 790 0 164 793 0 155 866 0
        procon.protected.zones.add ""01a5c331-9016-44a6-870e-6195d4908476gr"" ""levels/bc1_harvest_day_gr"" ""US_DEFENSE"" 4 130 811 0 132 799 0 118 798 0 117 811 0
        procon.protected.zones.add ""c3e2087c-ef19-42d6-a1f3-780f8deef83dgr"" ""levels/bc1_harvest_day_gr"" ""US_DEFENSE"" 4 95 809 0 97 797 0 108 798 0 107 810 0

        #Rush - Oasis
        procon.protected.zones.add ""9bdd26cb-8b69-465c-b1fc-b7e155dc29f1"" ""levels/bc1_oasis_gr"" ""US_BASE"" 4 -374 386 0 -452 339 0 -498 415 0 -419 458 0
        procon.protected.zones.add ""488b0b7a-f437-4fd6-9340-e18137fa0d54"" ""levels/bc1_oasis_gr"" ""US_DEFENSE"" 4 -418 448 0 -427 441 0 -420 430 0 -410 437 0
        procon.protected.zones.add ""a22e3259-4196-4dd1-8751-e7a04869a55a"" ""levels/bc1_oasis_gr"" ""US_DEFENSE"" 4 -401 388 0 -393 377 0 -382 384 0 -391 396 0

        #Rush - Cold War
        procon.protected.zones.add ""427f1477-6d4e-4227-99ad-c7ae15ad9432"" ""levels/mp_sp_002gr"" ""RU_BASE"" 16 46 -256 0 35 -259 0 23 -262 0 10 -257 0 -1 -248 0 -8 -238 0 -10 -226 0 -5 -211 0 -3 -195 0 1 -169 0 18 -163 0 35 -188 0 45 -202 0 58 -218 0 60 -233 0 56 -248 0

        #Rush - Phu Bai Valley
        procon.protected.zones.add ""c4cb6fa6-943a-40ae-8103-fa7616153a9r"" ""levels/nam_mp_006r"" ""US_BASE"" 21 141 280 0 166 276 0 175 239 0 169 195 0 160 182 0 146 178 0 131 176 0 116 179 0 97 180 0 85 184 0 66 194 0 63 189 0 56 182 0 47 182 0 45 190 0 50 199 0 57 211 0 62 229 0 65 249 0 86 274 0 115 280 0
        procon.protected.zones.add ""6830ad3d-4e38-475f-b935-47df13c16b6r"" ""levels/nam_mp_006r"" ""US_DEFENSE"" 4 127 181 0 136 181 0 136 190 0 127 190 0
        procon.protected.zones.add ""9b6fd076-9b10-4996-bcbb-325e80f86edr"" ""levels/nam_mp_006r"" ""US_DEFENSE"" 4 92 188 0 87 194 0 82 188 0 87 183 0
        procon.protected.zones.add ""5713a3f5-21c5-43ca-8281-c17308a81c4r"" ""levels/nam_mp_006r"" ""US_DEFENSE"" 4 59 189 0 53 194 0 48 187 0 55 183 0

        #Rush - Vantage Point
        procon.protected.zones.add ""9dbfacbd-e0c8-435d-a09a-162caba088er"" ""levels/nam_mp_002r"" ""NVA_BASE"" 15 -131 238 0 -120 234 0 -112 226 0 -107 214 0 -108 204 0 -114 195 0 -124 190 0 -135 190 0 -144 197 0 -148 206 0 -152 212 0 -155 221 0 -152 230 0 -147 234 0 -138 238 0

        #Rush - Hill 137
        procon.protected.zones.add ""558587c6-e904-4e7b-a521-d173a0e58bar"" ""levels/nam_mp_003r"" ""US_BASE"" 19 103 -26 0 113 -31 0 124 -31 0 162 -21 0 176 -23 0 187 -37 0 204 -68 0 198 -83 0 190 -93 0 181 -94 0 165 -100 0 142 -110 0 104 -111 0 86 -99 0 86 -84 0 79 -54 0 82 -42 0 89 -34 0 94 -28 0

        #Rush - Cao Son Temple
        procon.protected.zones.add ""5414fd38-fa16-4d23-b076-9890c942c15r"" ""levels/nam_mp_005r"" ""US_BASE"" 25 164 433 0 155 430 0 147 422 0 143 413 0 141 405 0 142 398 0 147 390 0 153 383 0 159 377 0 166 372 0 179 370 0 191 372 0 201 378 0 206 386 0 209 399 0 207 411 0 206 422 0 206 433 0 204 445 0 203 456 0 197 464 0 184 466 0 174 463 0 168 452 0 169 441 0
        
        #Rush - Operation Hastings
        procon.protected.zones.add ""5c9aa019-91ea-4942-9f05-4852e6e48d2c"" ""levels/nam_mp_007r"" ""US_BASE"" 18 -197 -160 0 -185 -160 0 -166 -167 0 -153 -168 0 -139 -163 0 -131 -148 0 -125 -129 0 -122 -116 0 -126 -106 0 -136 -101 0 -147 -104 0 -153 -114 0 -167 -113 0 -184 -112 0 -196 -115 0 -202 -123 0 -203 -137 0 -202 -151 0

        ";

        /*
        ######################################################################################
        ##   Team kill warnings 
        ######################################################################################
        ####
        #### This is list of warnings that are sent to team killer. 
        #### For each team kill, one warning is sent to the player who violated the rules. 
        ####
        #### You can have as many warnings as you want. 
        #### They are sent in the same order as listed.
        ####
        #### The following replacements can be used:
        ####
        #### %killer% - the name of the soldier that team killed
        #### %victim% - the name of the soldier that was team killed
        #### %count%  - the number of team kills by the killer
        ####
        */


        List<string> default_warn_team_kill = new List<string>(new string[]
        {
            "%killer%, no team killing please",
            "%killer%, you have two team kills!",
            "%killer%, you have %count% team kills!"
        });

        /*
        ######################################################################################
        ##   Base Raping Kill warnings 
        ######################################################################################
        ####
        #### This is list of warnings that are sent to base rapers. 
        #### For each base rape, one warning is sent to the player who violated the rules. 
        ####
        #### You can have as many warnings as you want. 
        #### They are sent in the same order as listed.
        ####
        #### The following replacements can be used:
        ####
        #### %killer% - the name of the soldier that base raped
        #### %victim% - the name of the soldier that was base raped
        #### %count%  - the number of base raping kills by the killer
        ####
        */


        List<string> default_warn_rape_kill = new List<string>(new string[]
        {
            "%killer%, no base raping please.",
            "%killer%, you have two base raping kills!",
            "%killer%, you have %count% base raping kills!"
        });

        /*
        ######################################################################################
        ##   Base Camping kill warnings 
        ######################################################################################
        ####
        #### This is list of warnings that are sent to base campers. 
        #### For each base camp kill, one warning is sent to the player who violated the rules. 
        ####
        #### You can have as many warnings as you want. 
        #### They are sent in the same order as listed.
        ####
        #### The following replacements can be used:
        ####
        #### %killer% - the name of the soldier who is base camping
        #### %victim% - the name of the soldier that was killed by the camper
        #### %count%  - the number of base camping kills by the killer
        ####
        */

        List<string> default_warn_camp_kill = new List<string>(new string[]
        {
            "%killer%, no base camping please.",
            "%killer%, move out of the base!",
            "%killer%, you have %count% base camping kills!"
        });

        /*
         ######################################################################################
         ##   Slaps 
         ######################################################################################
         ####
         #### This is a list of slaps that users can give each other with the '!slap' command.
         #### You can have as many types of slaps as you can come up with. Slaps are chosen at random.
         #### Slaps are publicly annouced.
         ####
         #### The variable 'slap_player' controls whether insulting is allowed or not.
         ####
        */

        List<string> default_slaps = new List<string>(new string[]
        {
             "%sender% slapped %receiver% with a big smelly tuna fish!",
             "%sender% bitch slapped %receiver%!",
             "%sender% slapped %receiver% to the back of the head!",
             "%sender% ninja slapped %receiver%!",
             "%sender% pimp slapped %receiver%!",
             "%sender% cheek slapped %receiver%!",
             "%sender% butt slapped %receiver%!"
        });

        /*
        ######################################################################################
        ##   Insults 
        ######################################################################################
        ####
        #### This is a list of insults that users can send to each other with the '!insult' command
        #### You can have as many insults as you want. Insults are chosen at random.  Only the sender 
        #### and the reciever of the insult see the message in the chat box.
        ####
        #### The variable 'insult_player' controls whether insulting is allowed or not.
        ####
        */

        List<string> default_insults = new List<string>(new string[]
        {
            "%name%, don't go away mad. Just go away.",
            "%name%, I'd like to help you out ... which way did you come in.",
            "%name%, you are so down to earth but not quite far down enough.",
            "%name%, you are living proof that manure can grow legs and walk.",
            "%name%, you are no longer beneath my contempt.",
            "%name%, tell me where your family tree is I'd like to cut it down!",
            "%name%, when I look into your eyes, I see the back of your head.",
            "%name%, I reprimanded my son for mimicking you. I told him not to act like a fool.",
            "%name%, make a mental note ... oh, I see you're out of paper!",
            "%name%, I hear you changed your mind! And got back two cents.",
            "%name%, I heard you got a brain transplant and the brain rejected you!",
            "%name%, I heard the doctors examined your head and found nothing.",
            "%name%, I know you are nobody's fool. You're everybody's.",
            "%name%, you are so ugly, the tide runs away from you.",
            "%name% is so ugly he went to the zoo and they kept him.",
            "%name% is so so ugly, he went to a freak show and got a permanent job.",
            "%name% fell out of the ugly tree, you hit every branch on the way down.",
            "%name%, in the morning I will be sober, but you will still be ugly!",
            "%name%, Is it Halloween? Oh ... that's your real face.",
            "%name%, don't turn the other cheek. It's just as ugly.",
            "%name%, your'so ugly the only place you're ever invited is outside."
        });

        /*
        ######################################################################################
        ##   Mocks 
        ######################################################################################
        ####
        #### This is a list of mockeries that are sent to soldiers when they suicide. 
        #### The suicide mockery message is chosen at random. 
        #### 
        #### The variable 'mock_suicide' controls whether soldiers are mocked on suicide or not.
        ####
        */

        List<string> default_mocks = new List<string>(new string[]
        {
            "You suicided, epic fail!",
            "You suicided, are you even trying soldier!",
            "You suicided, press Alt-F4 for auto-revive! "
        });




        Dictionary<string, string> map_mapping;

        bool plugin_enabled = false;
        string reading_points = "";
        float tresspass_percent = 0.5F;

        public Dictionary<string, bool> booleanVariables;
        public Dictionary<string, int> integerVariables;
        public Dictionary<string, float> floatVariables;
        public Dictionary<string, string> stringListVariables;
        public Dictionary<string, string> stringVariables;
        public Dictionary<string, string> enumVariables;
        public Dictionary<string, Type> enumVariablesType;
        public Dictionary<string, List<string>> variableGroups;
        public List<string> variableGroupsOrder;
        public List<string> hiddenVariables;


        private string map_file;


        private Dictionary<string, List<MapZone>> zones;
        private Dictionary<string, List<MapZone>> default_zones;
        private Dictionary<string, List<MapZone>> custom_zones;
        private MapZone selected_zone;
        private Dictionary<string, PlayerProfile> players;


        public enum ViolationType { camp_kill, rape_kill, team_kill, invalid };
        public enum ActionType { kick, punish, forgive, invalid, ban };
        private enum ZoneType { RU_BASE, RU_DEFENSE, US_BASE, US_DEFENSE, NVA_BASE, NVA_DEFENSE };

        /* - limbo: when the plugin is enabled for the first, time we do not know the state of the players if they are dead or alive
         * - left/kicked: after a player leaves, there might still be other objects referencing the object of the player that just left,
         *                and it's necessary to know that the player is not in-game anymore.
         * - violated: this is a special case of being dead. when you are violated, the victim died because the killer violated the rules.
         *             the purpose of this state is to avoid counting the same violation multiple times against the killer. (specially for overlapping base zones)
         */

        public enum PlayerState { dead, alive, left, kicked, limbo, violated, banned };
        public enum BanDuration { Temporary, Permanent, Round };
        public enum BanType { GUID, IPAddress, Name };
        public enum BooleanTrueFalse { True, False };



        private class PlayerPunishment
        {
            public PlayerProfile punisher;
            public PlayerProfile punished;
            public List<ViolationType> violations;

            public PlayerPunishment(PlayerProfile pr, PlayerProfile pd, List<ViolationType> v)
            {
                punisher = pr;
                punished = pd;
                violations = v;
            }
        }

        public enum MessageType { yell, say, invalid };

        private class PlayerMessage
        {
            public MessageType type;
            public string text;
            public int time;
            public PlayerMessage(string tx, int tm)
            {
                text = tx;
                time = tm;
                type = MessageType.yell;
            }

            public PlayerMessage(string tx)
            {
                text = tx;
                type = MessageType.say;
            }
        }

        private class PlayerProfile
        {
            private InsanePunisher_BC2 plugin;
            public string name;
            public PlayerState state;
            public CPlayerInfo info;
            public CPunkbusterInfo pbinfo;
            public Queue<PlayerMessage> qmsg;          //queued messages 
            public Queue<PlayerPunishment> qpun;       //queued punishments
            public Dictionary<PlayerProfile, int> rps; //RapeKills
            public Dictionary<PlayerProfile, int> tks; //TeamKills
            public Dictionary<PlayerProfile, int> cmp; //CampKills
            public Dictionary<PlayerProfile, int> vtk; //Vote kicks
            public Dictionary<PlayerProfile, int> vtb; //Vote bans

            public Dictionary<PlayerProfile, int> arps; //accumulated RapeKills
            public Dictionary<PlayerProfile, int> atks; //accumulated TeamKills
            public Dictionary<PlayerProfile, int> acmp; //accumulated CampKills

            public PlayerProfile killer;


            public PlayerProfile(InsanePunisher_BC2 plg, CPlayerInfo inf)
            {
                info = inf;
                pbinfo = new CPunkbusterInfo();
                name = info.SoldierName;
                plugin = plg;
                resetStats();
            }

            public PlayerProfile(InsanePunisher_BC2 plg, CPunkbusterInfo inf)
            {
                info = new CPlayerInfo();
                pbinfo = inf;
                name = pbinfo.SoldierName;
                plugin = plg;
                resetStats();
            }

            public PlayerProfile(InsanePunisher_BC2 plg, string nm)
            {
                name = nm;
                info = new CPlayerInfo();
                pbinfo = new CPunkbusterInfo();
                plugin = plg;
                resetStats();
            }


            public bool isUS()
            {
                return info.TeamID == 1;
            }

            public bool isRU()
            {
                return info.TeamID == 2;
            }

            public void resetStats()
            {
                //Pending (camping, team kill, rapes)
                cmp = new Dictionary<PlayerProfile, int>();
                tks = new Dictionary<PlayerProfile, int>();
                rps = new Dictionary<PlayerProfile, int>();


                //Accumulated (camping, team kill, rapes)
                acmp = new Dictionary<PlayerProfile, int>();
                atks = new Dictionary<PlayerProfile, int>();
                arps = new Dictionary<PlayerProfile, int>();

                //votes
                vtk = new Dictionary<PlayerProfile, int>();

                //queued punishments
                qpun = new Queue<PlayerPunishment>();

                //queued messages
                qmsg = new Queue<PlayerMessage>();

                //bans
                vtb = new Dictionary<PlayerProfile, int>();


                //other
                killer = null;
                state = PlayerState.limbo;
            }

            public void dequeueMessages()
            {
                int yell_delay = 0;
                while (this.qmsg.Count > 0)
                {
                    PlayerMessage msg = this.qmsg.Dequeue();
                    if (msg.type.Equals(MessageType.say))
                    {
                        this.plugin.SendPlayerMessage(name, msg.text);
                    }
                    else
                    {
                        //don't want to send yells back to back (wait for last yell to finish)
                        this.plugin.SendDelayedPlayerYell(name, msg.text, msg.time, yell_delay);
                        yell_delay += msg.time;
                    }

                }
            }

            public void enqueueMessage(PlayerMessage msg)
            {
                this.qmsg.Enqueue(msg);
            }

            public override string ToString()
            {
                return String.Format("{0,-25} st: {1,-5} tk: {2,-3} br: {3,-3} bc: {4,-3} vtk: {5,-3}, vtb: {6,-3}", name, state.ToString(), getTeamKills(), getRapeKills(), getCampKills(), getVotesReceived(), getBanVotesReceived());
                //return name + " st:" + state.ToString() + " tk:" + getTeamKills() + " br" + getRapeKills() + " bc" + getCampKills() + " vt" + getVotesReceived();
            }

            public void addTk(PlayerProfile victim)
            {
                if (!tks.ContainsKey(victim))
                    tks.Add(victim, 0);

                if (victim.isViolated())
                    return;
                else
                    victim.violatedBy(this);

                tks[victim]++;

                if (this.state.Equals(PlayerState.limbo))
                    this.state = PlayerState.alive;

                plugin.showActionCommands(victim, this, ViolationType.team_kill);
            }

            public void addCamp(PlayerProfile victim)
            {
                if (!cmp.ContainsKey(victim))
                    cmp.Add(victim, 0);

                if (victim.isViolated())
                    return;
                else
                    victim.violatedBy(this);

                cmp[victim]++;

                if (this.state.Equals(PlayerState.limbo))
                    this.state = PlayerState.alive;

                plugin.showActionCommands(victim, this, ViolationType.camp_kill);
            }

            public void addRape(PlayerProfile victim)
            {
                if (!rps.ContainsKey(victim))
                    rps.Add(victim, 0);

                if (victim.isViolated())
                    return;
                else
                    victim.violatedBy(this);

                rps[victim]++;

                if (this.state.Equals(PlayerState.limbo))
                    this.state = PlayerState.alive;

                plugin.showActionCommands(victim, this, ViolationType.rape_kill);
            }


            public void violatedBy(PlayerProfile killer)
            {
                this.state = PlayerState.violated;
                this.killedBy(killer);
            }

            public void killedBy(PlayerProfile k)
            {
                this.killer = k;
            }

            public PlayerProfile getLastKiller()
            {
                return this.killer;
            }

            public bool forgivenBy(PlayerProfile victim)
            {
                int violations = getPendingViolations(victim);

                if (violations == 0)
                {
                    //user cannot be forgiven unless a violation exists against the victim
                    plugin.SendPlayerMessage(victim.name, "you cannot forgive " + this.name + ", unless for " + plugin.getActionableViolationsStr(ActionType.forgive));
                    return false;
                }


                plugin.SendGlobalMessage(victim.name + " forgave " + this.name + " for " + getPendingViolationsStr(victim));
                //accumulateV(victim);

                return true;
            }

            public bool punishedBy(PlayerProfile victim)
            {
                if (plugin.isImmune(this) && !plugin.isImmune(victim))
                {
                    plugin.SendPlayerMessage(victim.name, " you cannot punish " + this.name + " because he has immunity");
                    return false;
                }

                int violations = getPendingViolations(victim);
                if (violations == 0)
                {
                    //user cannot be punished unless a violation exists against the victim
                    plugin.SendPlayerMessage(victim.name, "you cannot punish " + this.name + ", unless for " + plugin.getActionableViolationsStr(ActionType.punish));
                    return false;
                }

                if (this.isAlive())
                {

                    List<ViolationType> violationsList = getPendingViolationsList(victim);
                    List<ViolationType> auto_punishable_violations = new List<ViolationType>();
                    /* remove auto punishable violations from the list */
                    violationsList.RemoveAll(delegate (ViolationType violation_item)
                    {
                        if (plugin.getBooleanVarValue("auto_punish_" + violation_item.ToString()))
                        {
                            auto_punishable_violations.Add(violation_item);
                            return true;
                        }
                        return false;
                    });

                    /* send message for auto-punishable violations */
                    if (auto_punishable_violations.Count > 0)
                    {
                        string violations_str = plugin.list2string(violationsList2StringList(auto_punishable_violations), "and");
                        plugin.SendGlobalMessage(this.name + " was auto-punished for " + violations_str + " " + victim.name);
                    }
                    /* send message for non auto-punishable violations */
                    else if (violationsList.Count > 0)
                    {
                        string violations_str = plugin.list2string(violationsList2StringList(violationsList), "and");
                        plugin.SendGlobalMessage(victim.name + " punished " + this.name + " for " + getPendingViolationsStr(victim));
                    }

                    plugin.KillPlayer(this.name);
                }
                else if (this.isDead())
                {
                    plugin.SendPlayerMessage(victim.name, this.name + " " + plugin.playerstate2stringED(this.state) + ", your punishment was queued.");
                    queuePunishmentFrom(victim);
                }
                else
                {
                    plugin.SendPlayerMessage(victim.name, this.name + " " + plugin.playerstate2stringED(this.state) + ", and cannot be punished.");
                    return false;
                }


                accumulateV(victim);
                return true;
            }

            private void queuePunishmentFrom(PlayerProfile victim)
            {
                qpun.Enqueue(new PlayerPunishment(victim, this, getPendingViolationsList(victim)));
            }

            public void dequeuePunishment()
            {
                if (qpun.Count == 0)
                    return;

                PlayerPunishment pp = qpun.Dequeue();

                List<string> list = new List<string>();
                foreach (ViolationType violation in pp.violations)
                    list.Add(plugin.violation2stringING(violation));

                string incidents = plugin.list2string(list);

                plugin.SendGlobalMessage(pp.punisher.name + " punished " + pp.punished.name + " for earlier " + incidents + ".");
                plugin.KillPlayer(this.name);
            }

            public List<PlayerProfile> getQueuedPunishersList()
            {
                List<PlayerProfile> list = new List<PlayerProfile>();
                foreach (PlayerPunishment punishment in qpun)
                    list.Add(punishment.punisher);

                return list;
            }

            public int getQueuedPunishmentsCount()
            {
                return qpun.Count;
            }

            public string getQueuedPunishmentsStr(PlayerProfile victim)
            {

                foreach (PlayerPunishment punishment in qpun)
                {
                    if (punishment.punisher != victim)
                        continue;
                    List<string> list = new List<string>();
                    foreach (ViolationType violation in punishment.violations)
                        list.Add(plugin.violation2stringING(violation));

                    return plugin.list2string(list);
                }
                return "";
            }


            public int banVotedBy(PlayerProfile victim)
            {
                if (plugin.isImmune(this) && !plugin.isImmune(victim))
                {
                    plugin.SendPlayerMessage(victim.name, " you cannot vote ban " + this.name + " because he has immunity");
                    return 0;
                }

                int violations = getPendingViolations(victim);


                if (violations == 0)
                {
                    //user cannot be vote banned unless a violation exists against the victim
                    plugin.SendPlayerMessage(victim.name, "you cannot vote ban  " + this.name + ", unless for " + plugin.getActionableViolationsStr(ActionType.ban));
                    return 0;
                }


                string vstr = getPendingViolationsStr(victim);
                int votes = accumulateV(victim);

                if (this.wasKicked() || this.wasBanned())
                {
                    plugin.SendPlayerMessage(victim.name, this.name + " " + plugin.playerstate2stringED(this.state) + " already.");
                    return 0;
                }


                addBanVotesBy(victim, votes);


                int votesNeeded = plugin.getIntegerVarValue("max_ban_votes") - this.getBanVotesReceived() + 1;
                string votesNeededStr = "";
                if (votesNeeded > 1)
                    votesNeededStr = votesNeeded + " ban more votes needed.";
                else if (votesNeeded == 1)
                    votesNeededStr = votesNeeded + " ban more vote needed.";


                if (votes == 1)
                    plugin.SendGlobalMessage(victim.name + " voted to ban " + this.name + " for " + vstr + ". " + votesNeededStr);
                else if (votes > 1)
                    plugin.SendGlobalMessage(victim.name + " cast " + votes + " votes to ban " + this.name + " for " + vstr + ". " + votesNeededStr);


                //Alert other victims to vote
                List<PlayerProfile> other_victims = getVictims();
                foreach (PlayerProfile other_victim in other_victims)
                {
                    if (victim == other_victim)
                        continue;
                    plugin.SendPlayerMessage(other_victim.name, victim.name + " voted to ban " + this.name + ". Type: " + plugin.getActionCommand(ActionType.ban) + " " + this.name.Substring(0, 4));
                }

                return votes;
            }

            public int votedBy(PlayerProfile victim)
            {
                if (plugin.isImmune(this) && !plugin.isImmune(victim))
                {
                    plugin.SendPlayerMessage(victim.name, " you cannot vote kick " + this.name + " because he has immunity");
                    return 0;
                }

                int violations = getPendingViolations(victim);

                if (violations == 0)
                {
                    //user cannot be punished unless a violation exists against the victim
                    plugin.SendPlayerMessage(victim.name, "you cannot vote against " + this.name + ", unless for " + plugin.getActionableViolationsStr(ActionType.kick));
                    return 0;
                }


                string vstr = getPendingViolationsStr(victim);
                int votes = accumulateV(victim);

                if (this.wasKicked() || this.wasBanned())
                {
                    plugin.SendPlayerMessage(victim.name, this.name + " " + plugin.playerstate2stringED(this.state) + " already.");
                    return 0;
                }


                addVotesBy(victim, votes);


                int votesNeeded = plugin.getIntegerVarValue("max_kick_votes") - this.getVotesReceived() + 1;
                string votesNeededStr = "";
                if (votesNeeded > 1)
                    votesNeededStr = votesNeeded + " more votes needed.";
                else if (votesNeeded == 1)
                    votesNeededStr = votesNeeded + " more vote needed.";


                if (votes == 1)
                    plugin.SendGlobalMessage(victim.name + " voted to kick " + this.name + " for " + vstr + ". " + votesNeededStr);
                else if (votes > 1)
                    plugin.SendGlobalMessage(victim.name + " cast " + votes + " votes to kick " + this.name + " for " + vstr + ". " + votesNeededStr);


                //Alert other victims to vote
                List<PlayerProfile> other_victims = getVictims();
                foreach (PlayerProfile other_victim in other_victims)
                {
                    if (victim == other_victim)
                        continue;
                    plugin.SendPlayerMessage(other_victim.name, victim.name + " voted to kick " + this.name + ". Type: " + plugin.getActionCommand(ActionType.kick) + " " + this.name.Substring(0, 4));
                }

                return votes;
            }

            private int accumulateV(PlayerProfile victim)
            {
                int violations = 0;

                violations += accumulateRapeKillV(victim);
                violations += accumulateCampKillV(victim);
                violations += accumulateTeamKillV(victim);

                return violations;
            }

            private int accumulateRapeKillV(PlayerProfile victim)
            {
                if (rps.ContainsKey(victim))
                {
                    if (!arps.ContainsKey(victim))
                        arps.Add(victim, 0);

                    int v = rps[victim];
                    rps.Remove(victim);
                    arps[victim] += v;

                    return v;
                }

                return 0;
            }

            private int accumulateTeamKillV(PlayerProfile victim)
            {
                if (tks.ContainsKey(victim))
                {
                    if (!atks.ContainsKey(victim))
                        atks.Add(victim, 0);

                    int v = tks[victim];
                    tks.Remove(victim);
                    atks[victim] += v;

                    return v;
                }

                return 0;
            }

            private int accumulateCampKillV(PlayerProfile victim)
            {
                if (cmp.ContainsKey(victim))
                {
                    if (!acmp.ContainsKey(victim))
                        acmp.Add(victim, 0);

                    int v = cmp[victim];
                    cmp.Remove(victim);
                    acmp[victim] += v;

                    return v;
                }

                return 0;
            }

            private int addVotesBy(PlayerProfile victim, int v)
            {
                if (!vtk.ContainsKey(victim))
                    vtk.Add(victim, 0);

                vtk[victim] += v;

                return v;
            }

            private int addBanVotesBy(PlayerProfile victim, int v)
            {
                if (!vtb.ContainsKey(victim))
                    vtb.Add(victim, 0);

                vtb[victim] += v;

                return v;
            }

            public int getPendingViolations(PlayerProfile victim)
            {
                int kills = 0;

                kills += getPendingCampKills(victim);
                kills += getPendingTeamKills(victim);
                kills += getPendingRapeKills(victim);

                return kills;
            }



            public List<ViolationType> getPendingViolationsList(PlayerProfile victim)
            {
                List<ViolationType> list = new List<ViolationType>();
                if (getPendingCampKills(victim) > 0)
                    list.Add(ViolationType.camp_kill);
                if (getPendingRapeKills(victim) > 0)
                    list.Add(ViolationType.rape_kill);
                if (getPendingTeamKills(victim) > 0)
                    list.Add(ViolationType.team_kill);

                return list;
            }

            public List<ViolationType> getViolationsList()
            {
                List<ViolationType> list = new List<ViolationType>();
                if (getCampKills() > 0)
                    list.Add(ViolationType.camp_kill);
                if (getRapeKills() > 0)
                    list.Add(ViolationType.rape_kill);
                if (getTeamKills() > 0)
                    list.Add(ViolationType.team_kill);

                return list;
            }

            public string getPendingViolationsStr(PlayerProfile victim)
            {

                List<ViolationType> violations = getPendingViolationsList(victim);
                List<string> list = violationsList2StringList(violations);
                return plugin.list2string(list);
            }

            private List<string> violationsList2StringList(List<ViolationType> violations)
            {
                List<string> list = new List<string>();
                foreach (ViolationType violation in violations)
                    list.Add(plugin.violation2stringING(violation));

                return list;
            }

            public string getViolationsStr()
            {
                List<ViolationType> violations = getViolationsList();
                List<string> list = new List<string>();
                foreach (ViolationType violation in violations)
                    list.Add(plugin.violation2stringING(violation));
                return plugin.list2string(list);
            }


            public int getPendingCampKills(PlayerProfile victim)
            {
                if (cmp.ContainsKey(victim))
                    return cmp[victim];
                return 0;
            }

            public int getPendingTeamKills(PlayerProfile victim)
            {
                if (tks.ContainsKey(victim))
                    return tks[victim];
                return 0;
            }

            public int getPendingRapeKills(PlayerProfile victim)
            {
                if (rps.ContainsKey(victim))
                    return rps[victim];
                return 0;
            }

            public int getCampKills(PlayerProfile victim)
            {
                if (acmp.ContainsKey(victim))
                    return acmp[victim] + getPendingCampKills(victim);
                return 0;
            }

            public int getRapeKills(PlayerProfile victim)
            {
                if (arps.ContainsKey(victim))
                    return arps[victim] + getPendingRapeKills(victim);
                return 0;
            }

            public int getTeamKills(PlayerProfile victim)
            {
                if (atks.ContainsKey(victim))
                    return atks[victim] + getPendingTeamKills(victim);
                return 0;
            }

            public List<PlayerProfile> getCampKillVictims()
            {
                List<PlayerProfile> victims = new List<PlayerProfile>();
                foreach (KeyValuePair<PlayerProfile, int> pair in cmp)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                foreach (KeyValuePair<PlayerProfile, int> pair in acmp)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                return victims;
            }

            public List<PlayerProfile> getRapeKillVictims()
            {
                List<PlayerProfile> victims = new List<PlayerProfile>();
                foreach (KeyValuePair<PlayerProfile, int> pair in rps)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                foreach (KeyValuePair<PlayerProfile, int> pair in arps)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                return victims;
            }

            public List<PlayerProfile> getTeamKillVictims()
            {
                List<PlayerProfile> victims = new List<PlayerProfile>();
                foreach (KeyValuePair<PlayerProfile, int> pair in tks)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                foreach (KeyValuePair<PlayerProfile, int> pair in atks)
                    if (!victims.Contains(pair.Key))
                        victims.Add(pair.Key);
                return victims;
            }

            public List<PlayerProfile> getVictims()
            {
                List<PlayerProfile> victims = new List<PlayerProfile>();
                List<PlayerProfile> tk_victims = getTeamKillVictims();
                List<PlayerProfile> rp_victims = getRapeKillVictims();
                List<PlayerProfile> cp_victims = getCampKillVictims();

                foreach (PlayerProfile pp in tk_victims)
                    if (!victims.Contains(pp))
                        victims.Add(pp);

                foreach (PlayerProfile pp in rp_victims)
                    if (!victims.Contains(pp))
                        victims.Add(pp);

                foreach (PlayerProfile pp in cp_victims)
                    if (!victims.Contains(pp))
                        victims.Add(pp);

                return victims;
            }

            public bool punish(PlayerProfile killer)
            {
                return killer.punishedBy(this);
            }

            public bool forgive(PlayerProfile killer)
            {
                return killer.forgivenBy(this);
            }

            public int vote(PlayerProfile myKiller)
            {
                return myKiller.votedBy(this);
            }

            public int ban(PlayerProfile myKiller)
            {
                return myKiller.banVotedBy(this);
            }

            public int getVotesReceived()
            {
                int votes = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in vtk)
                    votes += pair.Value;

                return votes;
            }

            public int getBanVotesReceived()
            {
                int votes = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in vtb)
                    votes += pair.Value;

                return votes;
            }

            public int getVotesReceived(PlayerProfile victim)
            {
                if (vtk.ContainsKey(victim))
                    return vtk[victim];
                return 0;
            }

            public int getBanVotesReceived(PlayerProfile victim)
            {
                if (vtb.ContainsKey(victim))
                    return vtb[victim];
                return 0;
            }

            public List<PlayerProfile> getVoters()
            {
                List<PlayerProfile> voters = new List<PlayerProfile>();
                foreach (KeyValuePair<PlayerProfile, int> pair in vtk)
                    if (!voters.Contains(pair.Key))
                        voters.Add(pair.Key);
                return voters;
            }

            public List<PlayerProfile> getBanVoters()
            {
                List<PlayerProfile> voters = new List<PlayerProfile>();
                foreach (KeyValuePair<PlayerProfile, int> pair in vtb)
                    if (!voters.Contains(pair.Key))
                        voters.Add(pair.Key);
                return voters;
            }

            public int getViolations(ViolationType violation)
            {
                if (violation.Equals(ViolationType.camp_kill))
                    return getCampKills();
                else if (violation.Equals(ViolationType.rape_kill))
                    return getRapeKills();
                else if (violation.Equals(ViolationType.team_kill))
                    return getTeamKills();

                return 0;
            }


            public int getCampKills()
            {
                int camps = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in cmp)
                    camps += pair.Value;

                foreach (KeyValuePair<PlayerProfile, int> pair in acmp)
                    camps += pair.Value;

                return camps;
            }

            public int getRapeKills()
            {
                int rapes = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in rps)
                    rapes += pair.Value;

                foreach (KeyValuePair<PlayerProfile, int> pair in arps)
                    rapes += pair.Value;

                return rapes;
            }

            public int getTeamKills()
            {
                int tkills = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in tks)
                    tkills += pair.Value;

                foreach (KeyValuePair<PlayerProfile, int> pair in atks)
                    tkills += pair.Value;

                return tkills;
            }


            public int getViolations()
            {
                return getTeamKills() + getRapeKills() + getCampKills();
            }

            public int getPendingViolations()
            {
                return getPendingTeamKills() + gePendingtRapeKills() + getPendingCampKills();
            }


            public int getPendingCampKills()
            {
                int camps = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in cmp)
                    camps += pair.Value;

                return camps;
            }

            public int gePendingtRapeKills()
            {
                int rapes = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in rps)
                    rapes += pair.Value;

                return rapes;
            }

            public int getPendingTeamKills()
            {
                int tkills = 0;
                foreach (KeyValuePair<PlayerProfile, int> pair in tks)
                    tkills += pair.Value;

                return tkills;
            }





            public bool punish()
            {
                if (killer != null)
                    return punish(killer);

                return false;
            }

            public int vote()
            {
                if (killer != null)
                    return vote(killer);

                return 0;
            }

            public int ban()
            {
                if (killer != null)
                    return ban(killer);

                return 0;
            }

            public bool forgive()
            {
                if (killer != null)
                    return forgive(killer);

                return false;
            }

            public bool isAlive()
            {
                return state.Equals(PlayerState.alive);
            }

            public bool isDead()
            {
                return state.Equals(PlayerState.dead) || isViolated();
            }

            public bool wasKicked()
            {
                return state.Equals(PlayerState.kicked);
            }

            public bool wasBanned()
            {
                return state.Equals(PlayerState.banned);
            }

            public bool isViolated()
            {
                return state.Equals(PlayerState.violated);
            }
        }





        public InsanePunisher_BC2()
        {

            this.default_zones = new Dictionary<string, List<MapZone>>();
            this.custom_zones = new Dictionary<string, List<MapZone>>();


            this.players = new Dictionary<string, PlayerProfile>();

            this.booleanVariables = new Dictionary<string, bool>();


            this.booleanVariables.Add("say_rape_kill", false);
            this.booleanVariables.Add("say_team_kill", false);
            this.booleanVariables.Add("say_camp_kill", false);
            this.booleanVariables.Add("punish_team_kill", false);
            this.booleanVariables.Add("punish_rape_kill", false);
            this.booleanVariables.Add("punish_camp_kill", false);
            this.booleanVariables.Add("kick_team_kill", false);
            this.booleanVariables.Add("kick_rape_kill", false);
            this.booleanVariables.Add("kick_camp_kill", false);
            this.booleanVariables.Add("ban_team_kill", false);
            this.booleanVariables.Add("ban_rape_kill", false);
            this.booleanVariables.Add("ban_camp_kill", false);
            this.booleanVariables.Add("forgive_team_kill", false);
            this.booleanVariables.Add("forgive_rape_kill", false);
            this.booleanVariables.Add("forgive_camp_kill", false);
            this.booleanVariables.Add("auto_punish_rape_kill", false);
            this.booleanVariables.Add("auto_punish_team_kill", false);
            this.booleanVariables.Add("auto_punish_camp_kill", false);
            this.booleanVariables.Add("use_map_list", false);
            this.booleanVariables.Add("reset_map_list", false);
            //this.booleanVariables.Add("advanced_mode", false);


            /* Level File Names - Map Names */
            map_mapping = new Dictionary<string, string>();
            map_mapping.Add("levels/mp_001", "cq_panama_canal");
            map_mapping.Add("levels/mp_003", "cq_laguna_alta");
            map_mapping.Add("levels/mp_005", "cq_atacama_desert");
            map_mapping.Add("levels/mp_006cq", "cq_arica_harbor");
            map_mapping.Add("levels/mp_007", "cq_white_pass");
            map_mapping.Add("levels/mp_008cq", "cq_nelson_bay");
            map_mapping.Add("levels/mp_009cq", "cq_laguna_presa");
            map_mapping.Add("levels/mp_012cq", "cq_port_valdez");
            map_mapping.Add("levels/bc1_harvest_day_cq", "cq_harvest_day");
            map_mapping.Add("levels/bc1_oasis_cq", "cq_oasis");
            map_mapping.Add("levels/mp_sp_005cq", "cq_heavy_metal");
            map_mapping.Add("levels/nam_mp_002cq", "cq_vantage_point");
            map_mapping.Add("levels/nam_mp_003cq", "cq_hill_1337");
            map_mapping.Add("levels/nam_mp_005cq", "cq_cao_son_temple");
            map_mapping.Add("levels/nam_mp_006cq", "cq_phu_bai_valley");
            map_mapping.Add("levels/nam_mp_007cq", "cq_operation_hastings");
            map_mapping.Add("levels/mp_002", "gr_valparaiso");
            map_mapping.Add("levels/mp_004", "gr_isla_inocentes");
            map_mapping.Add("levels/mp_006", "gr_arica_harbor");
            map_mapping.Add("levels/mp_007gr", "gr_white_pass");
            map_mapping.Add("levels/mp_008", "gr_nelson_bay");
            map_mapping.Add("levels/mp_009gr", "gr_laguna_presa");
            map_mapping.Add("levels/mp_012gr", "gr_port_valdez");
            map_mapping.Add("levels/mp_005gr", "gr_atacama_desert");
            map_mapping.Add("levels/bc1_harvest_day_gr", "gr_harvest_day");
            map_mapping.Add("levels/bc1_oasis_gr", "gr_oasis");
            map_mapping.Add("levels/mp_sp_002gr", "gr_cold_war");
            map_mapping.Add("levels/nam_mp_002r", "gr_vantage_point");
            map_mapping.Add("levels/nam_mp_003r", "gr_hill_137");
            map_mapping.Add("levels/nam_mp_005r", "gr_cao_son_temple");
            map_mapping.Add("levels/nam_mp_006r", "gr_phu_bai_valley");
            map_mapping.Add("levels/nam_mp_007r", "gr_operation_hastings");


            this.booleanVariables.Add("mock_suicide", true);
            this.booleanVariables.Add("insult_player", true);
            this.booleanVariables.Add("slap_player", true);
            this.booleanVariables.Add("default_zones", true);
            this.booleanVariables.Add("debug_mode", false);
            this.booleanVariables.Add("quiet_mode", false);

            this.integerVariables = new Dictionary<string, int>();
            this.integerVariables.Add("max_ban_votes", 3);
            this.integerVariables.Add("ban_minutes", 20);
            this.integerVariables.Add("max_kick_votes", 3);
            this.integerVariables.Add("yell_rape_kill", 3);
            this.integerVariables.Add("yell_team_kill", 3);
            this.integerVariables.Add("yell_camp_kill", 3);
            this.integerVariables.Add("auto_punish_max", 0);


            this.floatVariables = new Dictionary<string, float>();
            this.floatVariables.Add("base_percent", 50);
            this.floatVariables.Add("def_percent", 15);

            this.stringListVariables = new Dictionary<string, string>();
            this.stringListVariables.Add("admin_list", @"micovery|admin2|admin3");
            this.stringListVariables.Add("player_whitelist", @"micovery|player2|player3");
            this.stringListVariables.Add("clan_whitelist", @"~IgA~|clan2|clan3");
            this.stringListVariables.Add("map_list", String.Join("|", new List<string>(this.map_mapping.Values).ToArray()));
            this.stringListVariables.Add("mock_msg_list", String.Join("|", this.default_mocks.ToArray()));
            this.stringListVariables.Add("insult_msg_list", String.Join("|", this.default_insults.ToArray()));
            this.stringListVariables.Add("slap_msg_list", String.Join("|", this.default_slaps.ToArray()));
            this.stringListVariables.Add("rape_kill_warn_list", String.Join("|", this.default_warn_rape_kill.ToArray()));
            this.stringListVariables.Add("team_kill_warn_list", String.Join("|", this.default_warn_team_kill.ToArray()));
            this.stringListVariables.Add("camp_kill_warn_list", String.Join("|", this.default_warn_camp_kill.ToArray()));



            this.stringVariables = new Dictionary<string, string>();

            this.enumVariables = new Dictionary<string, string>();
            this.enumVariablesType = new Dictionary<string, Type>();
            this.enumVariables.Add("ban_type", "GUID");
            this.enumVariablesType.Add("ban_type", typeof(BanType));
            this.enumVariables.Add("ban_duration", "Temporary");
            this.enumVariablesType.Add("ban_duration", typeof(BanDuration));



            this.hiddenVariables = new List<string>();

            this.hiddenVariables.Add("auto_punish_rape_kill");
            this.hiddenVariables.Add("auto_punish_team_kill");
            this.hiddenVariables.Add("auto_punish_camp_kill");

            this.hiddenVariables.Add("rape_kill_warn_list");
            this.hiddenVariables.Add("team_kill_warn_list");
            this.hiddenVariables.Add("camp_kill_warn_list");

            this.hiddenVariables.Add("mock_msg_list");
            this.hiddenVariables.Add("instul_msg_list");
            this.hiddenVariables.Add("slap_msg_list");
            this.hiddenVariables.Add("mock_msg_list");
            this.hiddenVariables.Add("instul_msg_list");
            this.hiddenVariables.Add("map_list");




            /* create variable groups */
            variableGroups = new Dictionary<string, List<string>>();

            variableGroups.Add("Base Raping", new List<string>());
            variableGroups["Base Raping"].Add("say_rape_kill");
            variableGroups["Base Raping"].Add("yell_rape_kill");
            variableGroups["Base Raping"].Add("punish_rape_kill");
            variableGroups["Base Raping"].Add("kick_rape_kill");
            variableGroups["Base Raping"].Add("ban_rape_kill");
            variableGroups["Base Raping"].Add("forgive_rape_kill");



            variableGroups.Add("Team Killing", new List<string>());
            variableGroups["Team Killing"].Add("say_team_kill");
            variableGroups["Team Killing"].Add("yell_team_kill");
            variableGroups["Team Killing"].Add("punish_team_kill");
            variableGroups["Team Killing"].Add("kick_team_kill");
            variableGroups["Team Killing"].Add("ban_team_kill");
            variableGroups["Team Killing"].Add("forgive_team_kill");



            variableGroups.Add("Base Camping", new List<string>());
            variableGroups["Base Camping"].Add("say_camp_kill");
            variableGroups["Base Camping"].Add("yell_camp_kill");
            variableGroups["Base Camping"].Add("punish_camp_kill");
            variableGroups["Base Camping"].Add("kick_camp_kill");
            variableGroups["Base Camping"].Add("ban_camp_kill");
            variableGroups["Base Camping"].Add("forgive_camp_kill");


            variableGroups.Add("Zones", new List<string>());
            variableGroups["Zones"].Add("default_zones");
            variableGroups["Zones"].Add("base_percent");
            variableGroups["Zones"].Add("def_percent");

            variableGroups.Add("Whitelists", new List<string>());
            variableGroups["Whitelists"].Add("clan_whitelist");
            variableGroups["Whitelists"].Add("player_whitelist");

            variableGroups.Add("Voting", new List<string>());
            variableGroups["Voting"].Add("max_ban_votes");
            variableGroups["Voting"].Add("max_kick_votes");

            variableGroups.Add("Banning", new List<string>());
            variableGroups["Banning"].Add("ban_type");
            variableGroups["Banning"].Add("ban_duration");
            variableGroups["Banning"].Add("ban_minutes");

            variableGroups.Add("Messages", new List<string>());
            variableGroups["Messages"].Add("mock_suicide");
            variableGroups["Messages"].Add("slap_player");
            variableGroups["Messages"].Add("insult_player");

            variableGroups.Add("Advanced", new List<string>());
            variableGroups["Advanced"].Add("auto_punish_rape_kill");
            variableGroups["Advanced"].Add("auto_punish_team_kill");
            variableGroups["Advanced"].Add("auto_punish_camp_kill");
            variableGroups["Advanced"].Add("auto_punish_max");
            variableGroups["Advanced"].Add("mock_msg_list");
            variableGroups["Advanced"].Add("slap_msg_list");
            variableGroups["Advanced"].Add("insult_msg_list");
            variableGroups["Advanced"].Add("rape_kill_warn_list");
            variableGroups["Advanced"].Add("team_kill_warn_list");
            variableGroups["Advanced"].Add("camp_kill_warn_list");
            variableGroups["Advanced"].Add("use_map_list");
            variableGroups["Advanced"].Add("reset_map_list");
            variableGroups["Advanced"].Add("map_list");



            /* Set the order of the groups */
            variableGroupsOrder = new List<string>();
            variableGroupsOrder.Add("Base Raping");
            variableGroupsOrder.Add("Team Killing");
            variableGroupsOrder.Add("Base Camping");
            variableGroupsOrder.Add("Zones");
            variableGroupsOrder.Add("Voting");
            variableGroupsOrder.Add("Banning");
            variableGroupsOrder.Add("Messages");
            variableGroupsOrder.Add("Whitelists");
            variableGroupsOrder.Add("Advanced");




        }


        public void loadSettings()
        {

            ConsoleWrite("loading settings");

            TextReader reader = new StringReader(settings);
            string line;
            while ((line = reader.ReadLine()) != null)
                this.parseSettingsLine(line);

            ConsoleWrite(getAllDefaaultZones().Count + " zones read");
        }


        public void unloadSettings()
        {
            this.default_zones.Clear();
            this.custom_zones.Clear();
            this.players.Clear();
        }

        public void parseSettingsLine(string line)
        {

            line = line.Trim();

            Match commentMatch = Regex.Match(line, @"^\s*#", RegexOptions.IgnoreCase);
            Match addZoneMatch = Regex.Match(line, @"procon.protected.zones.add\s+""([^""]+)""\s+""([^""]+)""\s+""([^""]+)""\s+(\d+)\s+(.+)");

            if (commentMatch.Success)
                return;

            if (addZoneMatch.Success)
            {
                string uid = addZoneMatch.Groups[1].Value;
                string map = addZoneMatch.Groups[2].Value;
                string tag = addZoneMatch.Groups[3].Value;
                string npointsStr = addZoneMatch.Groups[4].Value;
                string pointsStr = addZoneMatch.Groups[5].Value;

                int npoints;
                if (int.TryParse(npointsStr, out npoints) == false)
                {
                    ConsoleWrite("zone " + uid + " does not specify number of points");
                    return;
                }

                string[] pointsStrArr = Regex.Split(pointsStr.Trim(), @"\s+");
                if (pointsStrArr.Length != npoints * 3)
                {
                    ConsoleWrite("zone " + uid + " definition does not have " + npoints + " as specified");
                    return;
                }

                Point3D[] points = new Point3D[npoints];
                for (int i = 0; i % 3 == 0 && i < pointsStrArr.Length; i += 3)
                {
                    int x, y, z;

                    if (int.TryParse(pointsStrArr[i], out x) == false ||
                        int.TryParse(pointsStrArr[i + 1], out y) == false ||
                        int.TryParse(pointsStrArr[i + 2], out z) == false)
                    {
                        ConsoleWrite("point #" + ((i / 3) + 1) + " in zone " + uid + " is invalid");
                        return;
                    }
                    Point3D point = new Point3D(x, y, z);
                    points[(i / 3)] = point;

                }

                addDefaultZone(new MapZone(uid, map, tag, points, false));
            }

        }

        public string GetPluginName()
        {
            return "Insane Punisher";
        }

        public string GetPluginVersion()
        {
            return "0.0.1.0";
        }

        public string GetPluginAuthor()
        {
            return "micovery";
        }

        public string GetPluginWebsite()
        {
            return "www.insanegamersasylum.com";
        }


        public string GetPluginDescription()
        {
            return @"
        <h2>Description</h2>
        <p> This plugin is designed to discourage soldiers from base raping, base camping, and team killing. 
        </p>

        <h2>Map Zones</h2>
        <p> In order to set up this plugin, you should  designate the base areas and the base defense areas in the battle map. 
            Designating defense areas is not required but recommended. If you do not want to define these zones, you can use 
            the default zone definitions provided in the settings file by setting the variable ""default_zones"" to true.
            If you use your own map zones, these do not take effect immediately. Custom zones are detected the first time someone 
            dies or kills in it. To view the list of zones for the current map use the !zones command.
        </p>      
        <h4><u>US_BASE</u> / <u>RU_BASE</u> / <u>NVA_BASE</u></h4> 
                          
        <blockquote>
           This zone is used mainly for designating spawn areas. Soldiers are not allowed to shoot into
           this zone, or out from it unless they are in a defense zone. Air vehicles and ground vehicles are always shoot out from the base.
           Hopefully, this does not happen too often. In a future release, ground vehicles will be restricted in the amount of times
           they are allow to shoot out from the base before being punished.
        </blockquote>

        <h4><u>US_DEFENSE</u> / <u>RU_DEFENSE</u> / <u>NVA_DEFENSE</u></h4>                  
        <blockquote>
           This zone is used mainly for designating base defense areas such as Anti-Air and Stationary AT guns. 
           Soldiers cannot be punished for killing from this area. Also, soldiers cannot be punished for killing an
           enemy that is in a defense area (unless they already trespassed into the base).
           Note that defense zones should be inside or at least overlap with the base of the same team. 
        </blockquote>

        <h2>Rules of Engagement - Punishable Incidents</h2>
        
        <pre>    
            Legend
            --  base boundary
            --  defense boundary
  
        
                    Base Raping                                  Base Raping           
           (enemy shoots into base)             (enemy tespasses and shoots soldier in defense)
           +-------------------------+                  +-------------------------+
           ¦          ¦              ¦                  ¦          ¦      o       ¦
           ¦          ¦              ¦                  ¦     o <--¦------+>      ¦  
           ¦          ¦              ¦                  ¦    /|\   ¦      /\      ¦    
           ¦          ¦              ¦                  ¦    /\    ¦              ¦    
           ¦----------+              ¦           o      ¦----------+              ¦
           ¦     o <-----------------¦-----------+>     ¦                         ¦
           ¦    /|\                  ¦           /\     ¦                         ¦
           ¦    /\                   ¦                  ¦                         ¦
           +-------------------------+                  +-------------------------+

                   Base Raping                                Base Camping      
          (enemy trespasses into base)                (soldier shoots enemy from base) 
           +-------------------------+                  +-------------------------+
           ¦                         ¦                  ¦          ¦              ¦
           ¦                         ¦                  ¦          ¦              ¦ 
           ¦                         ¦                  ¦          ¦              ¦
           ¦                         ¦                  ¦          ¦              ¦
           ¦             o           ¦                  ¦----------+              ¦
           ¦     o <-----+>          ¦                  ¦    O                    ¦    \o/
           ¦    /|\      /\          ¦                  ¦    <+-------------------¦-- > |
           ¦    /\                   ¦                  ¦    /\                   ¦     /\
           +-------------------------+                  +-------------------------+


                  Team Killing
            (inside or outside base)  
           +-------------------------+
           ¦                         ¦
           ¦      o      \o_         ¦
           ¦      \=-->  /           ¦
           ¦      /\   //            ¦
           ¦                         ¦    o      \o_
           ¦                         ¦    \=-->  /
           ¦                         ¦    /\   //
           ¦                         ¦
           +-------------------------+

                                     
        </pre>  

        <h2>Rules of Engagement - Non-Punishable Incidents</h2>
        <pre>

                  Base Defense                                  Base Defense  
        (soldier shoots enemy from defense)           (soldier shoots enemy trespasser)
           +-------------------------+                  +-------------------------+
           ¦          ¦              ¦                  ¦          ¦              ¦
           ¦    0     ¦              ¦                  ¦          ¦              ¦   
           ¦   <+-----¦--------------¦---------> o      ¦          ¦              ¦    
           ¦   /\     ¦              ¦          /|\     ¦          ¦              ¦    
           ¦----------+              ¦          /\      ¦----------+              ¦           
           ¦                         ¦                  ¦    O            \o/     ¦    
           ¦                         ¦                  ¦    <+----------> |      ¦
           ¦                         ¦                  ¦    /\           /\      ¦
           +-------------------------+                  +-------------------------+

                 Attack on Defense 
    (enemy shoots into defense from outside base) 
           +-------------------------+
           ¦          ¦              ¦          o
           ¦     o <--¦--------------¦----------+>
           ¦    /|\   ¦              ¦          /\
           ¦    /\    ¦              ¦
           ¦----------+              ¦
           ¦                         ¦
           ¦                         ¦
           ¦                         ¦
           +-------------------------+

        </pre>
        <h2>Settings</h2>
        <ol>
          <li><blockquote><strong>debug_mode</strong><br />
                <i>true</i> - shows debugging information whenever kill/death happens in a base zone <br />
                <i>false</i> - no debugging information shown
                </blockquote> 
           </li>
          <li><blockquote><strong>base_percent</strong><br />
                <i>(float >= 0)</i> - percent of overlap between base zone and solider location  <br />
                a soldier's location is approximated by a cirlce of 10 meter radius 
                </blockquote> 
          </li> 
          <li><blockquote><strong>def_percent</strong><br />
                <i>(float >= 0)</i> -  pecent of overlap between defense zone and soldier location <br />
                a soldier's location is approximated by a cirlce of 10 meter radius 
                </blockquote> 
          </li> 
          <li><blockquote><strong>max_kick_votes</strong><br />
                <i>(integer > 0)</i> - maximum number of votes that a soldier can receive before being kicked. <br />
                e.g if max_kick_votes = 3, soldier is kicked that the 4th vote.  <br />
                </blockquote> 
          </li> 
          <li><blockquote><strong>max_ban_votes</strong><br />
                <i>(integer > 0)</i> - maximum number of votes that a soldier can receive before being banned. <br />
                e.g if max_ban_votes = 3, soldier is banned that the 4th vote.  <br />
                </blockquote> 
          </li> 
          <li><blockquote><strong>yell_rape_kill</strong><br />
               <i>(integer >= 0 )</i> - number of seconds to yell at killer and victim after base rape violation
               </blockquote> 
          </li>
          <li><blockquote> <strong>yell_camp_kill</strong><br />
               <i>(integer >= 0)</i> - number of seconds to yell at killer and victim after base camping violation 
               </blockquote> 
          </li>
          <li><blockquote> <strong>yell_team_kill</strong><br />
               <i>(integer >= 0) </i> - number of seconds to yell at killer and victim after team kill violation
               </blockquote> 
          </li>
          <li><blockquote> <strong>say_team_kill</strong><br />
                <i>true</i> - inform killer and victim (in chat box) of team kill violation <br />
                <i>false</i> - do not inform killer or victim of team kill violation  
                </blockquote> 
          </li>
          <li><blockquote><strong>say_rape_kill</strong><br />
                <i>true</i> -  inform killer and victim (in chat box) of base raping violation<br />
                <i>false</i> - do not inform killer or victim of base raping violation  
                </blockquote> 
          </li>
          <li><blockquote><strong>say_camp_kill</strong><br />
                <i>true</i> -  inform killer and victim (in chat box) of base camping violation  <br />
                <i>false</i> - do not inform killer or victim of base camping violation 
                </blockquote> 
          </li>
          <li><blockquote> <strong>punish_rape_kill</strong><br />
                <i>true</i> - allows victim to punish the base raper <br />
                <i>false</i> - forbids victim from punishing the base raper
                </blockquote> 
           </li>
           <li><blockquote><strong>punish_camp_kill</strong><br />
                <i>true</i> - allows victim to punish the base camper <br />
                <i>false</i> - forbids victim from punishing the base camper
                </blockquote> 
           </li>
           <li><blockquote><strong>punish_team_kill</strong><br />
                <i>true</i> - allows victim to punish the team killer <br />
                <i>false</i> - forbids victim from punishing the team killer
                </blockquote> 
           </li>
           <li><blockquote><strong>forgive_rape_kill</strong><br />
                <i>true</i> - allows victim to forgive the base raper <br />
                <i>false</i> - forbids victim from forgiving the base raper
                </blockquote> 
           </li>
           <li><blockquote><strong>forgive_camp_kill</strong><br />
                <i>true</i> - allows victim to forgive the base camper <br />
                <i>false</i> - forbids victim from forgiving the base camper
                </blockquote> 
           </li>
           <li><blockquote><strong>forgive_team_kill</strong><br />
                <i>true</i> - allows victim to forgive the team killer <br />
                <i>false</i> - forbids victim from forgiving the team killer
                </blockquote> 
           </li>
           <li><blockquote><strong>kick_rape_kill</strong><br />
                <i>true</i> - allows victim to vote kick the base raper <br />
                <i>false</i> - forbids victim from vote kicking against the base raper
                </blockquote> 
           </li>
           <li><blockquote><strong>kick_camp_kill</strong><br />
                <i>true</i> - allows victim to vote kick the base camper <br />
                <i>false</i> - forbids victim from vote kicking the base camper
                </blockquote> 
           </li>
           <li><blockquote><strong>kick_team_kill</strong><br />
                <i>true</i> - allows victim to vote kick the team killer <br />
                <i>false</i> - forbids victim from vote kicking the team killer
                </blockquote> 
           </li>
           <li><blockquote><strong>ban_rape_kill</strong><br />
                <i>true</i> - allows victim to vote ban the base raper <br />
                <i>false</i> - forbids victim from vote banning against the base raper
                </blockquote> 
           </li>
           <li><blockquote><strong>ban_camp_kill</strong><br />
                <i>true</i> - allows victim to vote ban the base camper <br />
                <i>false</i> - forbids victim from vote banning the base camper
                </blockquote> 
           </li>
           <li><blockquote><strong>ban_team_kill</strong><br />
                <i>true</i> - allows victim to vote ban the team killer <br />
                <i>false</i> - forbids victim from vote banning the team killer
                </blockquote> 
           </li>
           <li><blockquote><strong>mock_suicide</strong><br />
                <i>true</i> - server mocks the soldier that suicided (private message) <br />
                <i>false</i> - no message sent on suicide
                </blockquote> 
           </li>
           <li><blockquote><strong>slap_player</strong><br />
                <i>true</i> - allows soldiers to slap each other with a big smelly tuna fish <br />
                <i>false</i> - forbids soldiers from slapping each other 
                </blockquote> 
           </li>
           <li><blockquote><strong>insult_player</strong><br />
                <i>true</i> - allows soldiers to insult each other, random insult is sent privately<br />
                <i>false</i> - forbids soldiers from insulting each other
                </blockquote> 
           </li>
           <li><blockquote><strong>default_zones</strong><br />
                <i>true</i> - ignores battle map zones, and uses the default zones from the settings file<br />
                <i>false</i> - ignores zones from settings file, and uses zones from the battle map
                </blockquote> 
           </li>  
           <li><blockquote><strong>admin_list</strong><br />
                <i>(Stringlist)</i> - list of soldiers that are allowed to modify/view plugin settings while in-game.<br />
                </blockquote> 
           </li>
           <li><blockquote><strong>clan_whitelist</strong><br />
                <i>(Stringlist)</i> - list of clans that have immunity against punish, kick, and ban <br />
                </blockquote> 
           </li>
           <li><blockquote><strong>player_whitelist</strong><br />
                <i>(Stringlist)</i> - list of soldiers that have immunity against punish, kick, and ban <br />
                </blockquote> 
           </li>
           <li><blockquote><strong>ban_type</strong><br />
                <i>GUID</i> - ban players by EA GUID <br />
                <i>Name</i> - ban players by EA soldier name<br />
                <i>IPAddress</i> - ban players by IP Address <br />
                </blockquote> 
           </li> 
           <li><blockquote><strong>ban_duration</strong><br />
                <i>Permanent</i> - ban indefinitely <br />
                <i>Round</i> - ban until the end of the current round<br />
                <i>Temporary</i> - ban temporarily (you need to set <b>ban_minutes</b> as well)<br />
                </blockquote> 
           </li>
          <li><blockquote><strong>ban_minutes</strong><br />
                <i>(integer > 0)</i> - Number of minutes to ban a player after being vote banned <br />
                </blockquote> 
          </li>
         <!--
          <li><blockquote><strong>advanced_mode</strong><br />
                <i>true</i> - shows the advanced settings group <br />
                <i>false</i> - hides the advanced settings group 
                </blockquote> 
          </li>
         -->        
        </ol>
        <h2>Advanced Settings</h2>
        <p>
           Do not use these settings if you are just beginign to learn how to use this plugin. These settings allow you to tweak
           default behaviors for actions againts rule violators, as well as default messages.
        </p>
        <ol>
          <li><blockquote><strong>auto_punish_rape_kill</strong><br />
              <i>true</i> - base raper is automatically punished, and victim is not allowed to take any action <br />
              <i>false</i> - base raper is not automatically punished <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>auto_punish_team_kill</strong><br />
              <i>true</i> - team killer is automatically punished, and victim is not allowed to take any action <br />
              <i>false</i> - team killer is not automatically punished <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>auto_punish_camp_kill</strong><br />
              <i>true</i> - base camper is automatically punished, and victim is not allowed to take any action <br />
              <i>false</i> - bae camper is not automatically punished <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>auto_punish_max</strong><br />
              <i>(interger >= 0)</i> -  maximum number of punishments that a player can receive before being auto kicked<br />
              For example, if the value of <b>auto_punish_max</b> is 3, the player will be kicked on the 4th punishment received<br />
              If the value is 0, it means it's that auto kick is disabled. 
              </blockquote> 
          </li>
          Note that when <b>auto_punish_*</b> settings are enabled, then the corresponding setting <b>punish_*</b> is ignored.
          <li><blockquote><strong>use_map_list</strong><br />
              <i>true</i> - only maps that are on the <b>map_list</b> are used by the plugin <br />
              <i>false</i> - ignores the maps in the <b>map_list</b> <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>map_list</strong><br />
              <i>(StringList)</i> - list of maps that are used when <b> use_map_list</b> is enabled <br />
               Supported maps are:
               <ul>
                <li>cq_panama_canal</li>
                <li>cq_laguna_alta</li>
                <li>cq_atacama_desert</li>
                <li>cq_arica_harbor</li>
                <li>cq_white_pass</li>
                <li>cq_nelson_bay</li>
                <li>cq_laguna_presa</li>
                <li>cq_port_valdez</li>
                <li>cq_harvest_day</li>
                <li>cq_oasis</li>
                <li>cq_heavy_metal</li>
                <li>cq_vantage_point</li>
                <li>cq_hill_1337</li>
                <li>cq_cao_son_temple</li>
                <li>cq_phu_bai_valley</li>
                <li>gr_valparaiso</li>
                <li>gr_isla_inocentes</li>
                <li>gr_arica_harbor</li>
                <li>gr_white_pass</li>
                <li>gr_nelson_bay</li>
                <li>gr_laguna_presa</li>
                <li>gr_port_valdez</li>
                <li>gr_atacama_desert</li>
                <li>gr_harvest_day</li>
                <li>gr_oasis</li>
                <li>gr_cold_war</li>
                <li>gr_vantage_point</li>
                <li>gr_hill_137</li>
                <li>gr_cao_son_temple</li>
                <li>gr_phu_bai_valley</li>
               </ul>
               All supported maps are included by default in the list, if you wisht to selectively enable the plugin for certain maps </br>
               then enable <b>use_map_list</b> and leave only the ones you wish in the <b>map_list</b>
              </blockquote> 
          </li>
          <li><blockquote><strong>slap_msg_list</strong><br />
              <i>(StringList)</i> - list of messages used for the <b>!slap</b> command  <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>mock_msg_list</strong><br />
              <i>(StringList)</i> - list of messages used to mock a player after suicide  <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>insult_msg_list</strong><br />
              <i>(StringList)</i> - list of messages used for the <b>!insult</b> command  <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>rape_kill_warn_list</strong><br />
              <i>(StringList)</i> - ordered list of warning messages that are sent to base rapers after a violation. <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>team_kill_warn_list</strong><br />
              <i>(StringList)</i> - ordered list of warning messages that are sent to team killers after a violation. <br />
              </blockquote> 
          </li>
          <li><blockquote><strong>camp_kill_warn_list</strong><br />
              <i>(StringList)</i> - ordered list of warning messages that are sent to base campers after a violation. <br />
              </blockquote> 
          </li>
          <p>
              Note that the order of the messages in the warning lists is relevant. <br />
              The first message is sent when the first violation happends. <br />
              The second message is sent when the second violation happens, and so on.
          </p>      
        </ol>    

        <h2>Public In-Game Commands</h2>
        <p>
            In-game commands are messages typed into the game chat box, which have special meaning to the plugin. 
            Commands must start with one of the following characters: !,@, or /. This plugin interprets the following commands:
        </p>
        <ul>
           <li><blockquote><strong>!punish [name-substring]</strong><br />
              
               This is the main command, and most fun of all. It allows victims to punish soldiers who violated the rules of engagement. 
               If ""!punish"" is used without specifying the soldier name, then the last aggressor with ROE violation (against the victim) is punished.
               Punishments cannot be stacked. For example, if an aggressor team kills a victim twice, then the victim can only punish the aggressor once. 
               However, punishments by different victims are stackable. For example, if an aggressor team kills two victims, and both decide to punish him, 
               then the aggressor is punished twice consecutively. Note that if an aggressor is dead, punishments are queued and applied on re-spawn.
              
               </blockquote> 
           </li> 
           <li><blockquote> <strong>!votekick [name-substring]</strong><br />
               This is a lesser used command, but still powerful. It allows victims to cast votes against soldiers who violated the rules of engagement.
               If ""!votekick"" is used without specifying the soldier name, then the last aggressor with ROE violation (against the victim) is voted against.
               Votes are stackable. For example, if an aggressor team kills a victim twice, he can vote once against the aggressor, with two votes being cast.
               When a victim casts a kick vote against an aggressor, all other victims of that aggressor, who have not taken action, are reminded to vote.
               This is similar to a traditional votekick, with the difference that only victims of the aggressor are allowed to vote.
            </blockquote> 
           <li><blockquote> <strong>!voteban [name-substring]</strong><br />
               This is also a lesser used command, but extremely powerful. It allows victims to cast votes against soldiers who violated the rules of engagement.
               If ""!voteban"" is used without specifying the soldier name, then the last aggressor with ROE violation (against the victim) is voted against.
               Votes are stackable. For example, if an aggressor team kills a victim twice, he can vote once against the aggressor, with two votes being cast.
               When a victim casts a ban vote against an aggressor, all other victims of that aggressor, who have not taken action, are reminded to vote.
               This is similar to a traditional voteban, with the difference that only victims of the aggressor are allowed to vote.
            </blockquote> 
           </li>
           <li><blockquote> <strong>!forgive [name-substring]</strong><br />
               This is a command of camaraderie. It allows victims to forgive soldiers who violated the rules of engagement. When forgiving, the victim waives
               the right to take action against the aggressor for all previous ROE violatons. This command is mostly used by soldiers forgiving teammates afer an 
               accidental team kill. 
            </blockquote> 
           <li><blockquote> <strong>!slap [name-substring]</strong><br />
               This command is just for fun. It allows soldiers to slap each other publicly. The type of slap is chosen randomly from the settings file. 
            </blockquote> 
           <li><blockquote><strong>!insult [name-substring]</strong><br />
               This is another useless, but fun command. It allows soldiers to privately insult each other. The pre-defined insults in the settings file are mostly
               jokingly insults. Insults are chosen at random. 
            </blockquote> 
           <li><blockquote><strong>!mystats</strong><br />
               This is a statistics command. It allows a soldier to view his rules of engagement violations, such as team killing, base raping, and base camping.
               It displays the name of the victims, together with the total and pending violations against each victim. Pending violations are those
               for which the victim has not yet taken action.
            </blockquote> 
           </li>
        </ul>
       <h2> Admin In-Game Commands</h2>
        <p>
            These are the commands that only soldiers in the ""admin_list"" are allowed to execute. Reply messages generated by admin commands
            are sent only to the admin who executed the command.
        </p>
        <ul>
           <li><blockquote><strong>!settings</strong><br />
                This command prints the values of all plugin variables on the chat box. 
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>1. !set {variable} {to|=} {value}</strong><br />
                <strong>2. !set {variable} {value}</strong><br />       
                <strong>3. !set {variable}</strong><br />   
                This command is used for setting the value of this plugin's variables.<br />
                For the 2nd invocation syntax you cannot use ""="" or ""to"" as the variable value. <br />
                For the 3rd invocation syntax the value is assumed to be ""true"".
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>!get {variable} </strong><br />
                This command prints the value of the specified variable.
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>!enable {variable-substring}</strong><br />
                This command is a shortcut for enabling  (setting to ""true"") multiple variables at the same time.<br />
                For example, the following command: <br />
                !enable punish <br />
                results in all variables that contain the word ""punish"" being set to ""true""  e.g. <i>punish_rape_kill</i>, <i>punish_camp_kill</i>, and <i>punish_team_kill</i> 
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>!disable {variable-substring}</strong><br />
                This command is a shortcut for disabling  (setting to ""false"") multiple variables at the same time.<br />
                For example, the following command: <br />
                !disable punish <br />
                results in all variables that contain the word ""punish"" being set to ""false"" e.g. <i>punish_rape_kill</i>, <i>punish_camp_kill</i>, and <i>punish_team_kill</i> 
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>!stats [name-substring]</strong><br />
                This command is used for obtaining statistics about soldiers rules of engagement violations. <br />
                If no name is specified, it prints a summary of all soldiers' ROE violatons on the chat box. <br />
                If a name is specified, it prints a detailed report of that soldier's ROE violations. (similar to !mystats command)
               </blockquote> 
           </li>
           <li><blockquote>
                <strong>!zones</strong><br />
                This is a debugging command. It shows the list of zones active for the current map. It is useful when using custom map
                zones, to help you see the zones that have been detected.
               </blockquote> 
           </li>
         </ul> 
        ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            ConsoleWrite("plugin loaded");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bInsane Punisher ^2Enabled!");
            this.RegisterZoneTags("RU_BASE", "US_BASE", "RU_DEFENSE", "US_DEFENSE", "NVA_BASE", "NVA_DEFENSE");

            unloadSettings();
            loadSettings();

            plugin_enabled = true;

        }

        public void ConsoleWrite(string msg)
        {
            string prefix = "[^b" + GetPluginName() + "^n] ";
            this.ExecuteCommand("procon.protected.pluginconsole.write", prefix + msg);
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bInsane Punisher ^1Disabled =(");
            this.UnregisterZoneTags("RU_BASE", "US_BASE", "RU_DEFENSE", "US_DEFENSE", "NVA_BASE", "NVA_DEFENSE");

            plugin_enabled = false;

            unloadSettings();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();


            //lstReturn.Add(new CPluginVariable("Settings|Refresh", typeof(string), "| edit this field to refresh settings |"));


            List<string> vars = getPluginVars();
            foreach (string var in vars)
            {
                int temp;
                if (Regex.Match(var, @"^(quiet_mode)$").Success)
                    continue;

                string group = getPluginVarGroup(var);
                string type = "multiline";
                string value = getPluginVarValue(var);

                /* build boolean enum */
                if (isBooleanVar(var))
                    type = getEnumStr(var, typeof(BooleanTrueFalse));
                /* build the enum type for enums */
                else if (isEnumVar(var))
                    type = getEnumStr(var, getEnumVarType(var));
                else if (var.StartsWith("yell_"))
                {
                    /* for yell_* add the units */
                    value += " second";
                    int iValue = getIntegerVarValue(var);
                    if (iValue == 0 || iValue > 1)
                        value += "s";
                }

                if (variableGroupsOrder.Contains(group))
                    group = (variableGroupsOrder.IndexOf(group) + 1).ToString() + ". " + group;

                if (isStringListVar(var))
                    type = "stringarray";

                lstReturn.Add(new CPluginVariable(group + "|" + var, type, value));
            }

            return lstReturn;
        }


        public string getEnumStr(string var, Type type)
        {
            if (!type.IsEnum)
                return "";
            return "enum." + var + "(" + String.Join("|", Enum.GetNames(type)) + ")";
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            List<string> vars = getPluginVars();
            foreach (string var in vars)
                lstReturn.Add(new CPluginVariable(var, typeof(string), getPluginVarValue(var)));

            return lstReturn;
        }


        public void SetPluginVariable(string strVariable, string strValue)
        {
            //ConsoleWrite("setting " + strVariable + " to " + strValue);
            if (strVariable.ToLower().Contains("refresh"))
                return;
            setPluginVarValue(strVariable, strValue);
        }



        // Player events
        public void OnPlayerJoin(string strSoldierName)
        {

            if (!this.players.ContainsKey(strSoldierName))
                this.players.Add(strSoldierName, new PlayerProfile(this, strSoldierName));
        }


        public void OnPlayerLeft(string strSoldierName)
        {
            PlayerProfile player = getPlayerProfile(strSoldierName);
            if (player != null)
            {
                player.state = PlayerState.left;
                this.players.Remove(player.name);
            }
        }


        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (isInGameCommand(strMessage))
                inGameCommand(strSpeaker, strMessage);
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            if (isInGameCommand(strMessage))
                inGameCommand(strSpeaker, strMessage);
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            if (isInGameCommand(strMessage))
                inGameCommand(strSpeaker, strMessage);
        }

        private bool isInGameCommand(string str)
        {
            if (Regex.Match(str, @"^\s*[@/!]").Success)
                return true;

            return false;
        }

        public void OnPunkbusterplayerStatsCmd(CPunkbusterInfo cpbiPlayer)
        {

            if (cpbiPlayer != null)
            {
                if (this.players.ContainsKey(cpbiPlayer.SoldierName))
                    this.players[cpbiPlayer.SoldierName].pbinfo = cpbiPlayer;
                else
                    this.players.Add(cpbiPlayer.SoldierName, new PlayerProfile(this, cpbiPlayer));
            }
        }




        // Query Events
        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.map_file = csiServerInfo.Map.ToLower();
        }


        public void OnplayersStatsCmd(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.players.ContainsKey(cpiPlayer.SoldierName))
                        this.players[cpiPlayer.SoldierName].info = cpiPlayer;
                    else
                        this.players.Add(cpiPlayer.SoldierName, new PlayerProfile(this, cpiPlayer));
                }
            }
        }



        private void addPoint(string sender, Point3D point)
        {
            if (this.selected_zone == null)
            {
                SendConsoleMessage(sender, "cannot add point, no zone selected");
                return;
            }

            point.Z = 0;
            List<Point3D> points = new List<Point3D>(this.selected_zone.ZonePolygon);
            points.Add(point);

            SendConsoleMessage(sender, "point " + point2String(point) + " added");
            this.selected_zone.ZonePolygon = points.ToArray();
        }

        public void OnPlayerKilled(Kill killInfo)
        {

            if (!isMapEnabled())
                return;

            //get the killer and victim information

            CPlayerInfo killer = killInfo.Killer;
            CPlayerInfo victim = killInfo.Victim;

            PlayerProfile victimProfile = getPlayerProfile(victim.SoldierName);
            PlayerProfile killerProfile = getPlayerProfile(killer.SoldierName);


            if (this.reading_points.CompareTo(victim.SoldierName) == 0)
            {
                addPoint(victim.SoldierName, killInfo.VictimLocation);
            }


            //update the victim state
            if (killerProfile != null && victimProfile != null && !victimProfile.isViolated())
                victimProfile.killedBy(killerProfile);


            List<MapZone> zns = getCurrentMapZones();
            foreach (MapZone zn in zns)
            {
                //use only base zones
                if (!(isZoneType(ZoneType.US_BASE, zn) || isZoneType(ZoneType.RU_BASE, zn)))
                    continue;

                //ignore events outside bases
                if (!(isPointInZone(killInfo.VictimLocation, zn) > 0 || isPointInZone(killInfo.KillerLocation, zn) > 0))
                    continue;

                BaseEvent(killInfo, zn);
                break;

            }

            if (killInfo.IsSuicide)
            {

                //if (getBooleanVarValue("debug_mode"))
                //   SendGlobalMessage(victim.SoldierName + " suicided! " + point2String(killInfo.VictimLocation));

                /*
                if (killer.SoldierName.CompareTo("micovery") == 0)
                    TeamKill(killer, victim);
                */

                mockPlayerSuicide(victim.SoldierName);
                return;
            }
            else if (killer.TeamID == victim.TeamID)
            {
                TeamKill(killer, victim);
            }



        }


        public bool isMapEnabled()
        {
            string map = this.map_file.ToLower();
            if (!map_mapping.ContainsKey(map))
            {
                ConsoleWrite("^1map ^b" + map + "^n is not fully supported^0");
                return true;
            }

            if (!getBooleanVarValue("use_map_list"))
                return true;

            List<string> map_list = getStringListVarValue("map_list");


            string cur_map_name = map_mapping[map].Trim();
            foreach (string list_map_name in map_list)
                if (list_map_name.Trim().CompareTo(cur_map_name) == 0)
                    return true;

            return false;

        }

        public void OnRoundOver(int iWinningTeamID)
        {
            //reset all player statistics
            foreach (KeyValuePair<string, PlayerProfile> pair in this.players)
            {
                PlayerProfile player = pair.Value;
                player.resetStats();
            }
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            PlayerProfile playerProfile = getPlayerProfile(soldierName);
            if (playerProfile != null)
            {
                playerProfile.state = PlayerState.alive;

                playerProfile.dequeueMessages();
                playerProfile.dequeuePunishment();
            }
        }


        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
            this.map_file = mapFileName.ToLower();
        }


        private void KillPlayerWithMessage(string soldierName, string message)
        {
            SendPlayerMessage(soldierName, message);
            KillPlayer(soldierName);
        }

        private void KillPlayer(string soldierName)
        {
            PlayerProfile player = getPlayerProfile(soldierName);

            if (player != null && !player.isViolated())
                player.state = PlayerState.dead;

            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", soldierName);
        }


        private void KickPlayerWithMessage(string soldierName, string message)
        {

            PlayerProfile player = getPlayerProfile(soldierName);

            if (player != null)
            {
                player.state = PlayerState.kicked;
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", player.name, message);
            }
        }

        private void BanPlayerWithMessage(BanType type, BanDuration duration, String soldierName, int minutes, string message)
        {

            PlayerProfile player = getPlayerProfile(soldierName);
            if (player == null)
                return;

            /* get the type field and value */
            string typeField = "guid";
            string typeValue = player.info.GUID;

            if (type.Equals(BanType.GUID))
            {
                typeField = "guid";
                typeValue = player.info.GUID;
            }
            else if (type.Equals(BanType.IPAddress))
            {
                typeField = "ip";
                typeValue = player.pbinfo.Ip.Remove(player.pbinfo.Ip.IndexOf(':'));
            }
            else if (type.Equals(BanType.Name))
            {
                typeField = "name";
                typeValue = player.info.SoldierName;
            }

            /* get the time out value */
            string timeout = "seconds";
            if (duration.Equals(BanDuration.Permanent))
                timeout = "perm";
            else if (duration.Equals(BanDuration.Round))
                timeout = "round";
            else if (duration.Equals(BanDuration.Temporary))
                timeout = "seconds";

            if (duration.Equals(BanDuration.Temporary))
                message += " The ban duration is " + minutes + " minutes";
            else if (duration.Equals(BanDuration.Round))
                message += " The ban duration is until end of the round.";
            else if (duration.Equals(BanDuration.Permanent))
                message += " The ban duration is permanent.";


            player.state = PlayerState.banned;
            if (duration.Equals(BanDuration.Temporary))
                this.ExecuteCommand("procon.protected.send", "banList.add", typeField, typeValue, timeout, (minutes * 60).ToString(), message);
            else
                this.ExecuteCommand("procon.protected.send", "banList.add", typeField, typeValue, timeout, message);
        }


        private void KickPlayer(string soldierName)
        {

            PlayerProfile player = getPlayerProfile(soldierName);

            if (player != null)
            {
                player.state = PlayerState.kicked;
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", player.name, "");
            }
        }


        private void SendPlayerMessage(string soldierName, string message)
        {
            if (getBooleanVarValue("quiet_mode") && !isAdmin(soldierName))
                return;

            this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", soldierName);
        }

        private void SendPlayerYell(string soldierName, string message, int time)
        {
            if (getBooleanVarValue("quiet_mode"))
                return;
            PlayerProfile profile = getPlayerProfile(soldierName);

            if (profile == null)
                return;
            if (profile.isAlive())
                this.ExecuteCommand("procon.protected.send", "admin.yell", message, (((double)time) * 1000).ToString(), "player", soldierName);
            else
                profile.enqueueMessage(new PlayerMessage(message, time));
        }

        private void SendDelayedPlayerYell(string name, string message, int time, int delay)
        {
            this.ExecuteCommand("procon.protected.tasks.add", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", message, (((double)time) * 1000).ToString(), "player", name);
        }

        private void SendPlayerRespawnYell(string soldierName, string message, int time)
        {
            if (getBooleanVarValue("quiet_mode"))
                return;

            PlayerProfile profile = getPlayerProfile(soldierName);

            if (profile == null)
                return;

            profile.enqueueMessage(new PlayerMessage(message, time));
        }

        private void SendGlobalMessage(string message)
        {
            if (getBooleanVarValue("quiet_mode"))
                SendConsoleMessage(message);
            else
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");

        }

        private void SendConsoleMessage(string name, string msg)
        {
            if (name != null)
                SendPlayerMessage(name, msg);
        }

        private void SendConsoleMessage(string msg)
        {
            List<string> admin_list = getStringListVarValue("admin_list");
            foreach (string name in admin_list)
            {
                PlayerProfile pp = this.getPlayerProfile(name);
                if (pp != null)
                {
                    SendPlayerMessage(pp.name, msg);
                }
            }
        }


        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction za, MapZone zn, Point3D pntTresspassLocation, float flTresspassPercentage, object info)
        {
            if (!getBooleanVarValue("default_zones"))
                detectZone(zn);
        }


        public void BaseEvent(Kill kill, MapZone zn)
        {
            //get killer and victim info

            PlayerProfile killer = getPlayerProfile(kill.Killer);
            PlayerProfile victim = getPlayerProfile(kill.Victim);

            if (killer == null || victim == null)
            {
                SendConsoleMessage("unabled to find killer/victim profile");
                return;
            }

            /* 
             * Legend
             * K - killer
             * V - victim
             * O - own
             * E - enemy
             * B - base
             * D - defense
             * RU - russian
             * US - american
             * P - percent
             */


            //base meta data
            bool RU_B = isRUBase(zn);
            bool US_B = isUSBase(zn);


            //killer meta data
            bool K_US = killer.isUS();
            bool K_RU = killer.isRU();

            double K_RU_B_P = isInZone(kill.KillerLocation, ZoneType.RU_BASE);
            double K_US_B_P = isInZone(kill.KillerLocation, ZoneType.US_BASE);

            double K_RU_D_P = isInZone(kill.KillerLocation, ZoneType.RU_DEFENSE);
            double K_US_D_P = isInZone(kill.KillerLocation, ZoneType.US_DEFENSE);


            bool K_RU_B = K_RU_B_P > this.getFloatVarValue("base_percent");
            bool K_US_B = K_US_B_P > this.getFloatVarValue("base_percent");

            bool K_RU_D = K_RU_D_P > this.getFloatVarValue("def_percent");
            bool K_US_D = K_US_D_P > this.getFloatVarValue("def_percent");


            bool K_O_B = (K_RU && K_RU_B) ||
                         (K_US && K_US_B);

            bool K_E_B = (K_US && K_RU_B) ||
                         (K_RU && K_US_B);

            bool K_O_D = (K_US && K_US_D) ||
                         (K_RU && K_RU_D);

            bool K_E_D = (K_RU && K_US_D) ||
                         (K_US && K_RU_D);


            //victim meta data
            bool V_US = victim.isUS();
            bool V_RU = victim.isRU();


            double V_RU_B_P = isInZone(kill.VictimLocation, ZoneType.RU_BASE);
            double V_US_B_P = isInZone(kill.VictimLocation, ZoneType.US_BASE);

            double V_RU_D_P = isInZone(kill.VictimLocation, ZoneType.RU_DEFENSE);
            double V_US_D_P = isInZone(kill.VictimLocation, ZoneType.US_DEFENSE);


            bool V_RU_B = V_RU_B_P > this.getFloatVarValue("base_percent");
            bool V_US_B = V_US_B_P > this.getFloatVarValue("base_percent");

            bool V_RU_D = V_RU_D_P > this.getFloatVarValue("def_percent");
            bool V_US_D = V_US_D_P > this.getFloatVarValue("def_percent");


            bool V_O_B = (V_RU && V_RU_B) ||
                         (V_US && V_US_B);

            bool V_E_B = (V_US && V_RU_B) ||
                         (V_RU && V_US_B);

            bool V_O_D = (V_US && V_US_D) ||
                         (V_RU && V_RU_D);

            bool V_E_D = (V_RU && V_US_D) ||
                         (V_US && V_RU_D);


            //kill meta data
            bool AIR = isAirKill(kill);
            bool HEAVY = isHeavyVehicleKill(kill);
            bool STATIONARY = isStationaryKill(kill);
            bool LIGHT = isLightVehicleKill(kill);
            bool EXPL = isSecondaryExplosive(kill);
            bool INF = !(AIR || HEAVY || STATIONARY || LIGHT || EXPL);

            string bs = getTeamTag(zn);
            string killerLocation = (K_US_D_P > 0) ? "US_DEF" : (K_RU_D_P > 0) ? "RU_DEF" : (K_US_B_P > 0) ? "US_BASE" : (K_RU_B_P > 0) ? "RU_BASE" : "NA";
            string victimLocation = (V_US_D_P > 0) ? "US_DEF" : (V_RU_D_P > 0) ? "RU_DEF" : (V_US_B_P > 0) ? "US_BASE" : (V_RU_B_P > 0) ? "RU_BASE" : "NA";

            double killerPercent = (K_US_D_P > 0) ? K_US_D_P : (K_RU_D_P > 0) ? K_RU_D_P : (K_US_B_P > 0) ? K_US_B_P : (K_RU_B_P > 0) ? K_RU_B_P : 0;
            double victimPercent = (V_US_D_P > 0) ? V_US_D_P : (V_RU_D_P > 0) ? V_RU_D_P : (V_US_B_P > 0) ? V_US_B_P : (V_RU_B_P > 0) ? V_RU_B_P : 0;


            string killerTeam = (K_US) ? "US" : (K_RU) ? "RU" : "NA";
            string victimTeam = (V_US) ? "US" : (V_RU) ? "RU" : "NA";


            /* fix the strings for vietnam */
            if (isVietnam(zn))
            {
                killerTeam = killerTeam.Replace("RU", "NVA");
                victimTeam = victimTeam.Replace("RU", "NVA");
                killerLocation = killerLocation.Replace("RU", "NVA");
                victimLocation = victimLocation.Replace("RU", "NVA");
            }

            string kstr = "[" + killerTeam + "]" + killer.name + "@" + killerLocation + "(" + killerPercent + "%) <" + kill.DamageType.ToString() + "> [" + victimTeam + "]" + victim.name + "@" + victimLocation + "(" + victimPercent + "%)";
            string sstr = " <" + kill.DamageType.ToString() + "> [" + victimTeam + "]" + victim.name + "@" + victimLocation + "(" + victimPercent + "%)";




            string prefix = "";
            if (V_RU_B || V_RU_D || V_US_B || V_US_D)
                prefix = victimLocation + " ";



            if (kill.IsSuicide)
            {
                sstr = "(" + prefix + "SUICIDE): " + sstr;
                if (getBooleanVarValue("debug_mode"))
                    SendConsoleMessage(sstr);
                return;
            }
            else if ((K_US && V_US) || (K_RU && V_RU))
            {
                kstr = "(" + prefix + "TK): " + kstr;
                if (getBooleanVarValue("debug_mode"))
                    SendConsoleMessage(kstr);
                //ignore team killing
                return;
            }



            /* PUNISHABLE INCIDENTS */

            //base raping cases 
            while (true)
            {
                if ((V_O_B)) //victim in his own base
                {
                    //heli is allowed to kill people in defense zones
                    if (V_O_D && AIR)
                    {
                        kstr = "(" + bs + "_DEF AIR_RAID): " + kstr;

                        break;
                    }

                    //ground vehicles can shoot into defense zones from outside base
                    if (!K_E_B && V_O_D && HEAVY)
                    {
                        kstr = "(" + bs + "_DEF HEAVY_RAID): " + kstr;
                        break;
                    }

                    if (!K_E_B && V_O_D && INF)
                    {
                        kstr = "(" + bs + "_DEF INF_RAID): " + kstr;
                        break;
                    }

                    if (!K_E_B && V_O_D && LIGHT)
                    {
                        kstr = "(" + bs + "_DEF LIGHT_RAID): " + kstr;
                        break;
                    }

                    if (!K_E_B && V_O_D && HEAVY)
                    {
                        kstr = "(" + bs + "_DEF HEAVY_RAID): " + kstr;
                        break;
                    }

                    if (!K_E_B && V_O_D && EXPL)
                    {
                        kstr = "(" + bs + "_DEF EXPL_RAID): " + kstr;
                        break;
                    }

                    //base raping incident
                    kstr = "(" + bs + "_BASE RAPE): " + kstr;
                    BaseRapingKill(bs, killer.info, victim.info);
                }
                break;
            }

            //base camping
            while (true)
            {

                if (K_O_B && !(V_E_B || V_E_D))
                {
                    //vehicles, aa, at are allowed to base camp
                    if (AIR)
                    {
                        kstr = "(" + bs + "_BASE AIR_DEF): " + kstr;
                        break;
                    }

                    if (HEAVY)
                    {
                        kstr = "(" + bs + "_BASE HEAVY_DEF): " + kstr;
                        break;
                    }

                    /*if (LIGHT)
                    {
                        kstr = "(" + bs + "_BASE LIGHT_DEF): " + kstr;
                        break;
                    }*/

                    if (EXPL)
                    {
                        kstr = "(" + bs + "_BASE EXPL_DEF): " + kstr;
                        break;
                    }

                    if (STATIONARY)
                    {
                        kstr = "(" + bs + "_BASE STAT_DEF): " + kstr;
                        break;
                    }
                    if (INF && K_O_D)
                    {
                        kstr = "(" + bs + "_BASE INF_DEF): " + kstr;
                        break;
                    }

                    kstr = "(" + bs + "_BASE CAMP): " + kstr;
                    //base camping incident
                    BaseCampingKill(bs, killer.info, victim.info);
                }
                break;
            }

            if (getBooleanVarValue("debug_mode"))
                SendConsoleMessage(kstr);

        }



        private bool isInsaneZone(MapZone zn)
        {
            return isRUBase(zn) || isUSBase(zn) || isUSDefense(zn) || isRUDefense(zn);
        }

        private bool isUSBase(MapZone zn)
        {
            if (getZoneTags(zn).Contains("US_BASE"))
                return true;
            return false;
        }

        private bool isUSDefense(MapZone zn)
        {
            if (getZoneTags(zn).Contains("US_DEFENSE"))
                return true;
            return false;
        }

        private bool isRUBase(MapZone zn)
        {
            if (getZoneTags(zn).Contains("RU_BASE"))
                return true;
            return false;
        }

        private bool isRUDefense(MapZone zn)
        {
            if (getZoneTags(zn).Contains("RU_DEFENSE"))
                return true;
            return false;
        }

        private bool isUSSoldier(CPlayerInfo pl)
        {
            if (pl.TeamID.Equals(1))
                return true;
            else
                return false;
        }

        private bool isRUSoldier(CPlayerInfo pl)
        {
            if (pl.TeamID.Equals(2))
                return true;
            else
                return false;
        }


        private string getTeamTag(MapZone zn)
        {
            string tag = "";
            if (isRUBase(zn) || isRUDefense(zn))
                tag = "RU";
            else if (isUSBase(zn) || isUSDefense(zn))
                tag = "US";

            if (isVietnam(zn))
                tag = tag.Replace("RU", "NVA");

            return tag;
        }

        private bool isVietnam(MapZone zn)
        {
            return zn.LevelFileName.ToLower().Contains("nam_");
        }

        private bool isDeathAction(ZoneAction za)
        {
            if (za.Equals(ZoneAction.Death))
                return true;
            else
                return false;

        }

        private bool isKillAction(ZoneAction za)
        {
            if (za.Equals(ZoneAction.Kill))
                return true;
            else
                return false;
        }

        private class Point3DF
        {
            public double X;
            public double Y;
            public double Z;
            public Point3DF(Point3D point)
            {
                X = point.X;
                Y = point.Y;
                Z = point.Z;
            }
            public Point3DF(double x, double y, double z)
            {
                X = x; Y = y; Z = z;
            }

        }

        private double isPointInZone(Point3D point, MapZone zn)
        {
            int xmin, xmax, ymin, ymax;

            int count = 0;
            int total = 0;

            xmax = point.X + 10;
            ymax = point.Y + 10;
            for (xmin = point.X - 10; xmin < xmax; xmin++)
            {
                for (ymin = point.Y - 10; ymin < ymax; ymin++)
                {
                    if (isVisible(xmin, ymin, zn))
                    {
                        count++;

                    }
                    total++;
                }
            }

            double za = zoneArea(zn);

            //take the smallest of the two areas
            double smallest = (za < total) ? za : total;

            //calculate the percentage filled of the smallest area
            float percent = (float)(count * 100) / ((float)smallest);

            //SendConsoleMessage(zone2string(zn) + " - percent: " + percent + " zone area: "+ za );
            return Math.Round(percent, 2);

        }


        public double zoneArea(MapZone zn)
        {
            //cross product around the vetexes to find the area of the polygon
            Point3D[] pts = zn.ZonePolygon;
            double area = 0;
            int u, v, N = pts.Length;
            for (u = 0; u < N - 1; u++)
            {
                v = u + 1;
                area += pts[u].X * pts[v].Y - pts[u].Y * pts[v].X;
            }
            v = 0;
            area += pts[u].X * pts[v].Y - pts[u].Y * pts[v].X;
            return Math.Abs(area);
        }


        public bool isVisible(int x, int y, MapZone zn)
        {

            Point3DF[] poly = new Point3DF[zn.ZonePolygon.Length];
            for (int l = 0; l < poly.Length; l++)
                poly[l] = new Point3DF(zn.ZonePolygon[l]);

            int polySides = poly.Length;
            int i, j = polySides - 1;
            bool oddNodes = false;

            for (i = 0; i < polySides; i++)
            {
                if (poly[i].Y < y && poly[j].Y >= y || poly[j].Y < y && poly[i].Y >= y)
                    if (poly[i].X + (y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < x)
                        oddNodes = !oddNodes;
                j = i;
            }
            return oddNodes;
        }


        private double isInZone(Point3D point, ZoneType type)
        {
            double biggest = 0;
            double current = 0;
            List<MapZone> zones = getTeamZones(type);
            foreach (MapZone zn in zones)
                if (isZoneType(type, zn))
                    if ((current = isPointInZone(point, zn)) > biggest)
                        biggest = current;

            return biggest;

        }

        private bool isAirKill(Kill k)
        {

            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("mi28") || damage.Contains("ah60") || damage.Contains("mi24") ||
                damage.Contains("uav1") || damage.Contains("ah64"))
                return true;
            /* Vietnam Air Kill */
            else if (damage.Contains("hueyv"))
                return true;
            else
                return false;
        }

        private bool isProjectileExplosive(Kill k)
        {

            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("rpg7") || damage.Contains("m2cg") ||
                damage.Contains("m136"))
                return true;
            /* Vietnam Projectile Explosive */
            else if (damage.Contains("rpg7") || damage.Contains("m57v"))
                return true;
            else
                return false;
        }

        private bool isStationaryKill(Kill k)
        {
            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("zu23#cannons") || damage.Contains("korn#missile") || damage.Contains("tow2#launcher") ||
                damage.Contains("kord#gun") || damage.Contains("vads#auto") || damage.Contains("x307#gun"))
                return true;
            else
                return false;
        }

        private bool isHeavyVehicleKill(Kill k)
        {
            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("m3a3") || damage.Contains("bmd3") || damage.Contains("m1a2") ||
                damage.Contains("t90r"))
                return true;
            /* Vietnam Heavy */
            else if (damage.Contains("t54v") || damage.Contains("m48v"))
                return true;
            else
                return false;

        }

        private bool isSecondaryExplosive(Kill k)
        {
            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("atm-00") || damage.Contains("mrtr-5") || damage.Contains("dtn-4"))
                return true;
            /* Vietnam Explosives */
            else if (damage.Contains("mortarv"))
                return true;
            else
                return false;

        }



        private bool isLightVehicleKill(Kill k)
        {
            string damage = k.DamageType.ToString().ToLower();

            if (damage.Contains("cobr#") || damage.Contains("cavj#") || damage.Contains("humv#"))
                return true;
            /* Vietnam Light Vehicles */
            else if (damage.Contains("m151v#"))
                return true;
            else
                return false;
        }


        private List<MapZone> getTeamZones(ZoneType type)
        {
            List<MapZone> outzones = new List<MapZone>();
            List<MapZone> zns = getCurrentMapZones();
            foreach (MapZone zn in zns)
                if (isZoneType(type, zn))
                    outzones.Add(zn);

            return outzones;
        }


        private List<string> getZoneTags(MapZone zn)
        {
            List<string> tags = new List<string>(zn.Tags);
            for (int i = 0; i < tags.Count; i++)
                tags[i] = tags[i].Replace("NVA_", "RU_");

            return tags;
        }

        private bool isZoneType(ZoneType type, MapZone zn)
        {
            return getZoneTags(zn).Contains(type.ToString());
        }


        private string getDefenseZoneKey(MapZone zn)
        {
            return this.map_file + "." + zn.UID.ToString();
        }

        private void inGameCommand(string sender, string cmd)
        {

            //Zone definition

            Match saveZonesMatch = Regex.Match(cmd, @"\s*[!@/]\s*save zones?\s+(.+)\s*$", RegexOptions.IgnoreCase);
            Match mkZoneMatch = Regex.Match(cmd, @"\s*[!@/]\s*add zone\s+(.+)\s*$", RegexOptions.IgnoreCase);
            Match readPointsMatch = Regex.Match(cmd, @"\s*[!@/]\s*add points?\s*$", RegexOptions.IgnoreCase);
            Match closePointsMatch = Regex.Match(cmd, @"\s*[!@/]\s*end points?\s*$", RegexOptions.IgnoreCase);
            Match makeRectangleMatch = Regex.Match(cmd, @"\s*[!@/]\s*make rect(angle)?\s*$", RegexOptions.IgnoreCase);
            Match selectZoneMatch = Regex.Match(cmd, @"\s*[!@/]\s*zone\s+(\d+)", RegexOptions.IgnoreCase);
            Match dropZoneMatch = Regex.Match(cmd, @"\s*[!@/]\s*drop\s+zone\s+(\d+)", RegexOptions.IgnoreCase);
            Match dropPointMatch = Regex.Match(cmd, @"\s*[!@/]\s*drop\s*point\s*(\d+)", RegexOptions.IgnoreCase);

            Match listZonesMatch = Regex.Match(cmd, @"\s*[!@/]\s*zones", RegexOptions.IgnoreCase);
            Match listPointsMatch = Regex.Match(cmd, @"\s*[!@/]\s*points", RegexOptions.IgnoreCase);

            //Setting/Getting variables
            Match setVarValueMatch = Regex.Match(cmd, @"\s*[!@/]\s*set\s+([^ ]+)\s+(.+)", RegexOptions.IgnoreCase);
            Match setVarValueEqMatch = Regex.Match(cmd, @"\s*[!@/]\s*set\s+([^ ]+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
            Match setVarValueToMatch = Regex.Match(cmd, @"\s*[!@/]\s*set\s+([^ ]+)\s+to\s+(.+)", RegexOptions.IgnoreCase);
            Match setVarTrueMatch = Regex.Match(cmd, @"\s*[!@/]\s*set\s+([^ ]+)", RegexOptions.IgnoreCase);
            Match getVarValueMatch = Regex.Match(cmd, @"\s*[!@/]\s*get\s+([^ ]+)", RegexOptions.IgnoreCase);
            Match enableMatch = Regex.Match(cmd, @"\s*[!@/]\s*enable\s+(.+)", RegexOptions.IgnoreCase);
            Match disableMatch = Regex.Match(cmd, @"\s*[!@/]\s*disable\s+(.+)", RegexOptions.IgnoreCase);


            //Player actions2
            Match punishMatch = Regex.Match(cmd, @"\s*[!@/]\s*punish", RegexOptions.IgnoreCase);
            Match punishPlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*punish\s+(.+)", RegexOptions.IgnoreCase);
            Match voteMatch = Regex.Match(cmd, @"\s*[!@/]\s*votekick", RegexOptions.IgnoreCase);
            Match votePlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*votekick\s+(.+)", RegexOptions.IgnoreCase);
            Match forgiveMatch = Regex.Match(cmd, @"\s*[!@/]\s*forgive", RegexOptions.IgnoreCase);
            Match forgivePlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*forgive\s+(.+)", RegexOptions.IgnoreCase);
            Match banMatch = Regex.Match(cmd, @"\s*[!@/]\s*voteban", RegexOptions.IgnoreCase);
            Match banPlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*voteban\s+(.+)", RegexOptions.IgnoreCase);


            //Information
            Match playerStatsMatch = Regex.Match(cmd, @"\s*[!@/]\s*stats\s+([^ ].*)", RegexOptions.IgnoreCase);
            Match playersStatsMatch = Regex.Match(cmd, @"\s*[!@/]\s*stats", RegexOptions.IgnoreCase);
            Match pluginSettingsMatch = Regex.Match(cmd, @"\s*[!@/]\s*settings", RegexOptions.IgnoreCase);

            Match myStatsMatch = Regex.Match(cmd, @"\s*[!@/]\s*mystats", RegexOptions.IgnoreCase);
            Match slapPlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*slap\s+([^ ]+)", RegexOptions.IgnoreCase);
            Match insultPlayerMatch = Regex.Match(cmd, @"\s*[!@/]\s*insult\s+([^ ]+)", RegexOptions.IgnoreCase);


            bool senderIsAdmin = isAdmin(sender);



            if (listZonesMatch.Success && senderIsAdmin)
                llistZonesCmd(sender);
            else if (listPointsMatch.Success && senderIsAdmin)
                listPointsCmd(sender);
            else if (selectZoneMatch.Success && senderIsAdmin)
                selectZoneCmd(sender, selectZoneMatch.Groups[1].Value);
            else if (dropZoneMatch.Success && senderIsAdmin)
                dropZoneCmd(sender, dropZoneMatch.Groups[1].Value);
            else if (dropPointMatch.Success && senderIsAdmin)
                dropPointCmd(sender, dropPointMatch.Groups[1].Value);
            else if (playerStatsMatch.Success && senderIsAdmin)
                playerStatsCmd(sender, playerStatsMatch.Groups[1].Value);
            else if (playersStatsMatch.Success && senderIsAdmin)
                playersStatsCmd(sender);
            else if (setVarValueEqMatch.Success && senderIsAdmin)
                setVariableCmd(sender, setVarValueEqMatch.Groups[1].Value, setVarValueEqMatch.Groups[2].Value);
            else if (setVarValueToMatch.Success && senderIsAdmin)
                setVariableCmd(sender, setVarValueToMatch.Groups[1].Value, setVarValueToMatch.Groups[2].Value);
            else if (setVarValueMatch.Success && senderIsAdmin)
                setVariableCmd(sender, setVarValueMatch.Groups[1].Value, setVarValueMatch.Groups[2].Value);
            else if (setVarTrueMatch.Success && senderIsAdmin)
                setVariableCmd(sender, setVarTrueMatch.Groups[1].Value, "1");
            else if (getVarValueMatch.Success && senderIsAdmin)
                getVariableCmd(sender, getVarValueMatch.Groups[1].Value);
            else if (enableMatch.Success && senderIsAdmin)
                enableVarGroupCmd(sender, enableMatch.Groups[1].Value);
            else if (disableMatch.Success && senderIsAdmin)
                disableVarGroupCmd(sender, disableMatch.Groups[1].Value);
            else if (pluginSettingsMatch.Success && senderIsAdmin)
                pluginSettingsCmd(sender);
            else if (punishPlayerMatch.Success)
                punishMyKillerCmd(sender, punishPlayerMatch.Groups[1].Value);
            else if (punishMatch.Success)
                punishMyKillerCmd(sender);
            else if (votePlayerMatch.Success)
                voteMyKillerCmd(sender, votePlayerMatch.Groups[1].Value);
            else if (voteMatch.Success)
                voteMyKillerCmd(sender);
            else if (forgivePlayerMatch.Success)
                forgiveMyKillerCmd(sender, forgivePlayerMatch.Groups[1].Value);
            else if (forgiveMatch.Success)
                forgiveMyKillerCmd(sender);
            else if (banPlayerMatch.Success)
                banMyKillerCmd(sender, banPlayerMatch.Groups[1].Value);
            else if (banMatch.Success)
                banMyKillerCmd(sender);
            else if (myStatsMatch.Success)
                myStatsCmd(sender);
            else if (slapPlayerMatch.Success)
                slapPlayerCmd(sender, slapPlayerMatch.Groups[1].Value);
            else if (insultPlayerMatch.Success)
                insultPlayerCmd(sender, insultPlayerMatch.Groups[1].Value);
            else if (mkZoneMatch.Success && senderIsAdmin)
                mkZoneCmd(sender, mkZoneMatch.Groups[1].Value);
            else if (readPointsMatch.Success && senderIsAdmin)
                readPointsCmd(sender);
            else if (closePointsMatch.Success && senderIsAdmin)
                closePointsCmd(sender);
            else if (saveZonesMatch.Success)
                saveZonesCmd(sender, saveZonesMatch.Groups[1].Value);
            else if (makeRectangleMatch.Success && senderIsAdmin)
                makeRectangleCmd(sender);


        }

        private void slapPlayerCmd(string sender, string victim)
        {
            List<PlayerProfile> playerList = getPlayersProfile(victim);
            List<String> slap_list = getStringListVarValue("slap_msg_list");

            if (getBooleanVarValue("slap_player") == false)
            {
                SendPlayerMessage(sender, "slapping players is not allowed");
                return;
            }
            else if (slap_list.Count == 0)
            {
                SendPlayerMessage(sender, " server does not know any slaps.");
                return;
            }
            else if (playerList.Count > 1)
            {
                SendPlayerMessage(sender, " found multiple players matching \"" + victim + "\"");
                return;
            }
            else if (playerList.Count == 0)
            {
                SendPlayerMessage(sender, "cannot find player matching \"" + victim + "\"");
                return;
            }

            PlayerProfile pp = playerList[0];


            /*if (sender.CompareTo(pp.name) == 0)
            {
                SendPlayerMessage(sender, "you cannot slap yourself!");
                return;
            }*/

            Random rnd = new Random();


            string slap_text = slap_list[rnd.Next(slap_list.Count)];

            slap_text = slap_text.Replace("%sender%", sender);
            slap_text = slap_text.Replace("%receiver%", pp.name);

            SendGlobalMessage(slap_text);
        }

        private void insultPlayerCmd(string sender, string victim)
        {
            List<PlayerProfile> playerList = getPlayersProfile(victim);
            List<String> insult_list = getStringListVarValue("insult_msg_list");

            if (getBooleanVarValue("insult_player") == false)
            {
                SendPlayerMessage(sender, "insulting players is not allowed");
                return;
            }
            else if (insult_list.Count == 0)
            {
                SendPlayerMessage(sender, " server does not know any insults.");
                return;
            }
            else if (playerList.Count > 1)
            {
                SendPlayerMessage(sender, " found multiple players matching \"" + victim + "\"");
                return;
            }
            else if (playerList.Count == 0)
            {
                SendPlayerMessage(sender, "cannot find player matching \"" + victim + "\"");
                return;
            }

            PlayerProfile pp = playerList[0];


            /*if (sender.CompareTo(pp.name) == 0)
            {
                SendPlayerMessage(sender, "you cannot insult yourself!");
                return;
            }*/

            Random rnd = new Random();

            string insult_text = insult_list[rnd.Next(insult_list.Count)];

            insult_text = insult_text.Replace("%name%", pp.name);

            SendPlayerMessage(sender, insult_text);
            SendPlayerMessage(pp.name, sender + " says: " + insult_text);
        }

        private void mockPlayerSuicide(string name)
        {
            if (!getBooleanVarValue("mock_suicide"))
                return;

            List<String> mock_list = getStringListVarValue("mock_msg_list");

            if (mock_list.Count == 0)
                return;


            Random rnd = new Random();

            string mock_text = mock_list[rnd.Next(mock_list.Count)];
            mock_text = mock_text.Replace("%name%", name);

            SendPlayerMessage(name, mock_text);
        }

        private void myStatsCmd(string sender)
        {
            playerStatsCmd(sender, sender);
        }

        private void enableVarGroupCmd(string sender, string group)
        {
            enablePluginVarGroup(sender, group);
        }

        private void disableVarGroupCmd(string sender, string group)
        {
            disablePluginVarGroup(sender, group);
        }

        private bool setPluginVarGroup(string sender, string group, string val)
        {
            if (group == null)
            {
                SendPlayerMessage(sender, "no variables to enable");
                return false;
            }


            group = group.Replace(";", ",");
            List<string> vars = new List<string>(Regex.Split(group, @"\s*,\s*", RegexOptions.IgnoreCase));
            foreach (string var in vars)
            {
                if (setPluginVarValue(sender, var, val))
                    SendPlayerMessage(sender, var + " set to \"" + val + "\"");

            }
            return true;
        }

        private bool enablePluginVarGroup(string sender, string group)
        {
            //search for all variables matching
            List<string> vars = getVariableNames(group);
            if (vars.Count == 0)
            {
                SendPlayerMessage(sender, "no variables match \"" + group + "\"");
                return false;
            }

            return setPluginVarGroup(sender, String.Join(",", vars.ToArray()), "true");
        }

        private List<string> getVariableNames(string group)
        {
            List<string> names = new List<string>();
            List<string> list = new List<string>(Regex.Split(group, @"\s*,\s*"));
            List<string> vars = getPluginVars();
            foreach (string search in list)
            {
                foreach (string var in vars)
                {
                    if (var.Contains(search))
                        if (!names.Contains(var))
                            names.Add(var);
                }
            }

            return names;
        }

        private bool disablePluginVarGroup(string sender, string group)
        {
            //search for all variables matching
            List<string> vars = getVariableNames(group);
            if (vars.Count == 0)
            {
                SendConsoleMessage(sender, "no variables match \"" + group + "\"");
                return false;
            }
            return setPluginVarGroup(sender, String.Join(",", vars.ToArray()), "false");
        }

        private void getVariableCmd(string sender, string var)
        {
            string val = getPluginVarValue(sender, var);
            if (val != null)
                SendPlayerMessage(sender, var + " = " + val);
        }

        public ViolationType getViolationType(string str)
        {
            if (str == null)
                return ViolationType.invalid;

            if (Regex.Match(str, @"\s*camp_kill\s*", RegexOptions.IgnoreCase).Success)
                return ViolationType.camp_kill;
            else if (Regex.Match(str, @"\s*team_kill\s*", RegexOptions.IgnoreCase).Success)
                return ViolationType.team_kill;
            else if (Regex.Match(str, @"\s*rape_kill\s*", RegexOptions.IgnoreCase).Success)
                return ViolationType.rape_kill;
            else
                return ViolationType.invalid;
        }

        public ActionType getActionType(string str)
        {
            if (str == null)
                return ActionType.invalid;

            if (Regex.Match(str, @"\s*votekick\s*", RegexOptions.IgnoreCase).Success)
                return ActionType.kick;
            else if (Regex.Match(str, @"\s*voteban\s*", RegexOptions.IgnoreCase).Success)
                return ActionType.ban;
            else if (Regex.Match(str, @"\s*punish\s*", RegexOptions.IgnoreCase).Success)
                return ActionType.punish;
            else if (Regex.Match(str, @"\s*forgive\s*", RegexOptions.IgnoreCase).Success)
                return ActionType.forgive;
            else
                return ActionType.invalid;
        }

        private void setVariableCmd(string sender, string var, string val)
        {
            if (setPluginVarValue(sender, var, val))
                SendPlayerMessage(sender, var + " set to \"" + val + "\"");
        }

        private void pluginSettingsCmd(string sender)
        {
            SendConsoleMessage(sender, " == Insane Punisher == ");
            foreach (string var in getPluginVars(false))
            {
                /* do not show advanced variables in the in-game list */
                if (this.hiddenVariables.Contains(var))
                    continue;

                SendConsoleMessage(sender, var + " = " + getPluginVarValue(sender, var));
            }
        }


        private bool setPluginVarValue(string var, string val)
        {
            return setPluginVarValue(null, var, val);
        }


        private void resetMapList()
        {
            setPluginVarValue("map_list", String.Join("|", new List<string>(this.map_mapping.Values).ToArray()));
            ConsoleWrite("map list has been reset with " + this.map_mapping.Keys.Count + " map names");
        }

        private bool setPluginVarValue(string sender, string var, string val)
        {
            if (var == null || val == null)
                return false;

            if (!getPluginVars().Contains(var))
            {
                SendConsoleMessage(sender, "Insane Punisher: unknown variable \"" + var + "\"");
                return false;
            }

            /* Parse Boolean Values */
            bool booleanValue = false;
            bool isBooleanValue = true;
            if (Regex.Match(val, @"\s*(1|true|yes)\s*", RegexOptions.IgnoreCase).Success)
                booleanValue = true;
            else if (Regex.Match(val, @"\s*(0|false|no)\s*", RegexOptions.IgnoreCase).Success)
                booleanValue = false;
            else
                isBooleanValue = false;


            /* Parse Integer Values */
            int integerValue = 0;
            bool isIntegerValue = int.TryParse(val, out integerValue) && integerValue >= 0;


            /* Parse Float Values */
            float floatValue = 0F;
            bool isFloatValue = float.TryParse(val, out floatValue) && floatValue >= 0F;

            /* Parse String List */
            List<string> stringListValue = new List<string>(Regex.Split(val, @"\s*\|\s*"));
            bool isStringList = true;

            /* Parse String var */
            string stringValue = val;
            bool isStringValue = (val != null);

            /* Parse Enum var */
            string enumValue = val;
            bool isEnumValue = (val != null);


            if (isBooleanVar(var))
            {
                if (!isBooleanValue)
                {
                    SendConsoleMessage(sender, "\"" + val + "\" is invalid for " + var);
                    return false;
                }

                if (var.Equals("reset_map_list") && booleanValue)
                {
                    resetMapList();
                    booleanValue = false;
                }

                setBooleanVarValue(var, booleanValue);
                return true;
            }
            else if (isIntegerVar(var))
            {
                if (!isIntegerValue)
                {
                    SendConsoleMessage(sender, "\"" + val + "\" is invalid for " + var);
                    return false;
                }

                setIntegerVarValue(var, integerValue);
                return true;
            }
            else if (isFloatVar(var))
            {
                if (!isFloatValue)
                {
                    SendConsoleMessage(sender, "\"" + val + "\" is invalid for " + var);
                    return false;
                }

                setFloatVarValue(var, floatValue);
                return true;
            }
            else if (isStringListVar(var))
            {
                if (!isStringList)
                {
                    SendConsoleMessage(sender, "\"" + val + "\"  is invalid for " + var);
                    return false;
                }

                setStringListVarValue(var, stringListValue);
                return true;
            }
            else if (isStringVar(var))
            {
                if (!isStringValue)
                {
                    SendConsoleMessage(sender, "invalid value for " + var);
                    return false;
                }

                setStringVarValue(var, stringValue);
                return true;
            }
            else if (isEnumVar(var))
            {
                if (!isEnumValue)
                {
                    SendConsoleMessage(sender, "invalid value for " + var);
                    return false;
                }

                setEnumVarValue(var, enumValue);
                return true;
            }
            else
            {
                SendConsoleMessage(sender, "unknown variable \"" + var + "\"");
                return false;
            }

        }

        private bool isIntegerVar(string var)
        {
            return this.integerVariables.ContainsKey(var);
        }

        private int getIntegerVarValue(string var)
        {
            if (!isIntegerVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return -1;
            }

            return this.integerVariables[var];
        }

        private bool setIntegerVarValue(string var, int val)
        {
            if (!isIntegerVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }

            this.integerVariables[var] = val;
            return true;
        }


        private bool isStringVar(string var)
        {
            return this.stringVariables.ContainsKey(var);
        }


        private string getStringVarValue(string var)
        {
            if (!isStringVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return "";
            }

            return this.stringVariables[var];
        }

        private bool setStringVarValue(string var, string val)
        {
            if (!isStringVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }


            string oldval = this.stringVariables[var];
            this.stringVariables[var] = val;

            return true;
        }

        private bool isStringListVar(string var)
        {
            return this.stringListVariables.ContainsKey(var);
        }

        private List<string> getStringListVarValue(string var)
        {
            if (!isStringListVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return new List<string>();
            }

            string[] out_list = CPluginVariable.DecodeStringArray(this.stringListVariables[var]);
            return new List<string>(out_list);
        }

        private bool setStringListVarValue(string var, List<string> val)
        {
            if (!isStringListVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }

            List<string> cleanList = new List<string>();
            foreach (string item in val)
                if (Regex.Match(item, @"^\s*$").Success)
                    continue;
                else
                    cleanList.Add(item);

            //this.stringListVariables[var] = val;
            this.stringListVariables[var] = String.Join("|", val.ToArray());
            return true;
        }


        private bool isFloatVar(string var)
        {
            return this.floatVariables.ContainsKey(var);
        }

        private float getFloatVarValue(string var)
        {
            if (!isFloatVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return -1F;
            }

            return this.floatVariables[var];
        }

        private bool setFloatVarValue(string var, float val)
        {
            if (!isFloatVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }

            this.floatVariables[var] = val;
            return true;
        }


        private bool isBooleanVar(string var)
        {
            return this.booleanVariables.ContainsKey(var);
        }

        private bool getBooleanVarValue(string var)
        {
            if (!isBooleanVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }

            return this.booleanVariables[var];
        }

        private bool setBooleanVarValue(string var, bool val)
        {
            if (!isBooleanVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }

            this.booleanVariables[var] = val;
            return true;
        }


        private bool isEnumVar(string var)
        {
            return this.enumVariables.ContainsKey(var);
        }


        private string getEnumVarValue(string var)
        {
            if (!isEnumVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return "";
            }

            return this.enumVariables[var];
        }

        private Object getEnumVarValueObj(string var)
        {
            if (!isEnumVar(var))
                return null;

            /* get the enum type */
            Type type = getEnumVarType(var);
            if (type == null)
                return null;

            /* get the enum value */
            string value = getEnumVarValue(var);
            if (value.Length == 0)
                return null;

            return System.Enum.Parse(type, value, true);
        }

        private bool setEnumVarValue(string var, string val)
        {
            if (!isEnumVar(var))
            {
                SendConsoleMessage("unknown variable \"" + var + "\"");
                return false;
            }


            string oldval = this.enumVariables[var];
            this.enumVariables[var] = val;

            return true;
        }

        public Type getEnumVarType(string var)
        {
            if (!isEnumVar(var) || !this.enumVariablesType.ContainsKey(var))
                return null;

            return this.enumVariablesType[var];
        }



        private string getPluginVarValue(string var)
        {
            return getPluginVarValue(null, var);
        }

        private string getPluginVarValue(string sender, string var)
        {
            if (!getPluginVars().Contains(var))
            {
                SendConsoleMessage(sender, "Insane Punisher: unknown variable \"" + var + "\"");
                return "";
            }

            if (isBooleanVar(var))
            {
                return getBooleanVarValue(var).ToString();
            }
            else if (isIntegerVar(var))
            {
                return getIntegerVarValue(var).ToString();
            }
            else if (isFloatVar(var))
            {
                return getFloatVarValue(var).ToString();
            }
            else if (isStringListVar(var))
            {
                string lst = String.Join("|", getStringListVarValue(var).ToArray());
                return lst;
            }
            else if (isStringVar(var))
            {
                return getStringVarValue(var);
            }
            else if (isEnumVar(var))
            {
                return getEnumVarValue(var);
            }
            else
            {
                SendConsoleMessage(sender, "Insane Punisher: unknown variable \"" + var + "\"");
                return "";
            }
        }


        private string getPluginVarGroup(string varname)
        {
            if (varname == null || varname.Length == 0)
                return "Other";

            foreach (KeyValuePair<string, List<string>> pair in variableGroups)
                foreach (string cmp_varname in pair.Value)
                    if (cmp_varname.CompareTo(varname) == 0)
                        return pair.Key;

            return "Other";
        }


        private List<string> getPluginVars()
        {
            return getPluginVars(true);
        }

        private List<string> getPluginVars(bool show_advanced)
        {
            List<string> vars = new List<string>();

            vars.AddRange(getIntegerPluginVars());
            vars.AddRange(getBooleanPluginVars());
            vars.AddRange(getStringListPluginVars());
            vars.AddRange(getFloatPluginVars());
            vars.AddRange(getStringPluginVars());
            vars.AddRange(getEnumPluginVars());

            if (!(show_advanced /* || getBooleanVarValue("advanced_mode")*/))
            {
                foreach (string advanced_var in variableGroups["Advanced"])
                    vars.Remove(advanced_var);
            }


            return vars;
        }

        private List<string> getEnumPluginVars()
        {
            return new List<string>(this.enumVariables.Keys);
        }

        private List<string> getStringPluginVars()
        {
            return new List<string>(this.stringVariables.Keys);
        }


        private List<string> getStringListPluginVars()
        {
            return new List<string>(this.stringListVariables.Keys);
        }


        private List<string> getIntegerPluginVars()
        {
            return new List<string>(this.integerVariables.Keys);
        }

        private List<string> getFloatPluginVars()
        {
            return new List<string>(this.floatVariables.Keys);
        }

        private List<string> getBooleanPluginVars()
        {
            return new List<string>(this.booleanVariables.Keys);
        }


        private void punishMyKillerCmd(string victimName)
        {
            punishMyKillerCmd(victimName, null);
        }

        private List<PlayerProfile> getVictimAndKillerProfiles(string victimName, string killerName, ActionType action)
        {
            List<PlayerProfile> profiles = new List<PlayerProfile>();

            //get the victim profile
            PlayerProfile victimProfile = getPlayerProfile(victimName);
            if (victimProfile == null)
            {
                SendPlayerMessage(victimName, " unable to find your player profile.");
                return null;
            }

            //get the killer's profile
            List<PlayerProfile> killerProfiles;

            if (killerName == null)
            {
                killerProfiles = new List<PlayerProfile>();
                if (victimProfile.getLastKiller() == null)
                {
                    SendPlayerMessage(victimName, " there is nobody to " + action.ToString() + " at this time.");
                    return null;
                }
                else
                    killerProfiles.Add(victimProfile.getLastKiller());
            }
            else
                killerProfiles = getPlayersProfile(killerName);

            int count = killerProfiles.Count;

            //there are no killers under that name
            if (count == 0)
            {
                SendPlayerMessage(victimProfile.name, "cannot find player matching " + killerName);
                return null;
            }

            profiles.Add(victimProfile);
            profiles.AddRange(killerProfiles);

            return profiles;
        }

        private void punishMyKillerCmd(string victimName, string killerName)
        {
            if (getActionableViolations(ActionType.punish).Count == 0)
            {
                SendPlayerMessage(victimName, action2stringING(ActionType.punish) + " is not allowed");
                return;
            }

            List<PlayerProfile> profiles = getVictimAndKillerProfiles(victimName, killerName, ActionType.punish);

            if (profiles == null)
                return;

            PlayerProfile victimProfile = profiles[0];
            profiles.RemoveAt(0);

            List<PlayerProfile> killerProfiles = profiles;


            //there is one or more killers under that name
            foreach (PlayerProfile killerProfile in killerProfiles)
                if (victimProfile.punish(killerProfile))
                    enforcePendingAutoKicks(killerProfile);
        }


        private void voteMyKillerCmd(string victimName)
        {
            voteMyKillerCmd(victimName, null);
        }

        private void voteMyKillerCmd(string victimName, string killerName)
        {
            if (getActionableViolations(ActionType.kick).Count == 0)
            {
                SendPlayerMessage(victimName, action2stringING(ActionType.kick) + " is not allowed");
                return;
            }

            List<PlayerProfile> profiles = getVictimAndKillerProfiles(victimName, killerName, ActionType.kick);

            if (profiles == null)
                return;

            PlayerProfile victimProfile = profiles[0];
            profiles.RemoveAt(0);

            List<PlayerProfile> killerProfiles = profiles;

            //there is one or more killers under that name
            foreach (PlayerProfile killerProfile in killerProfiles)
                if (victimProfile.vote(killerProfile) > 0)
                    enforcePendingKicks(killerProfile);
        }


        private void enforcePendingKicks(PlayerProfile killer)
        {
            int votesRec = killer.getVotesReceived();
            string violations = killer.getViolationsStr();


            if (getBooleanVarValue("kick_rape_kill") || getBooleanVarValue("kick_camp_kill") || getBooleanVarValue("kick_team_kill"))
            {
                /* vote kicking is enabled for at least one type of violation */
                if (votesRec > getIntegerVarValue("max_kick_votes"))
                {
                    SendGlobalMessage(killer.name + " was vote kicked for " + violations);
                    KickPlayerWithMessage(killer.name, " vote kicked for " + violations);
                }
                else
                {
                    if (votesRec == getIntegerVarValue("max_kick_votes"))
                        SendGlobalMessage(killer.name + " is one vote away from being vote kicked!");
                    else
                        SendPlayerMessage(killer.name, "You are " + (getIntegerVarValue("max_kick_votes") - votesRec + 1) + " votes away from being vote kicked!");
                }
            }
        }


        private void enforcePendingAutoKicks(PlayerProfile killer)
        {
            string violations = killer.getViolationsStr();
            int auto_max = getIntegerVarValue("auto_punish_max");
            int vcount = (killer.getViolations() - killer.getPendingViolations()); /* do not count pending violations */

            if (auto_max > 0)
            {
                /* auto kicking is enabled */
                if (vcount > auto_max)
                {
                    SendGlobalMessage(killer.name + " was auto kicked for " + violations);
                    KickPlayerWithMessage(killer.name, " auto kicked for " + violations);
                }
                else if (vcount == auto_max)
                    SendGlobalMessage(killer.name + " is one violation away from being auto kicked!");
                else
                    SendPlayerMessage(killer.name, "You are " + (auto_max - vcount) + " violation away from being auto kicked!");
            }
        }

        private void enforcePendingBans(PlayerProfile killer)
        {
            int votesRec = killer.getBanVotesReceived();
            string violations = killer.getViolationsStr();


            if (votesRec > getIntegerVarValue("max_ban_votes"))
            {
                SendGlobalMessage(killer.name + " was vote banned for " + violations);
                int time = getIntegerVarValue("ban_minutes");
                BanPlayerWithMessage((BanType)getEnumVarValueObj("ban_type"), (BanDuration)getEnumVarValueObj("ban_duration"), killer.name, time, " vote banned for " + violations + ".");
            }
            else
            {
                if (votesRec == getIntegerVarValue("max_ban_votes"))
                    SendGlobalMessage(killer.name + " is one vote away from being banned!");
                else
                    SendPlayerMessage(killer.name, "You are " + (getIntegerVarValue("max_ban_votes") - votesRec + 1) + " votes away from being banned!");
            }
        }


        private void banMyKillerCmd(string victimName)
        {
            banMyKillerCmd(victimName, null);
        }

        private void banMyKillerCmd(string victimName, string killerName)
        {
            if (getActionableViolations(ActionType.ban).Count == 0)
            {
                SendPlayerMessage(victimName, action2stringING(ActionType.ban) + " is not allowed");
                return;
            }

            List<PlayerProfile> profiles = getVictimAndKillerProfiles(victimName, killerName, ActionType.ban);

            if (profiles == null)
                return;

            PlayerProfile victimProfile = profiles[0];
            profiles.RemoveAt(0);

            List<PlayerProfile> killerProfiles = profiles;

            //there is one or more killers under that name
            foreach (PlayerProfile killerProfile in killerProfiles)
                if (victimProfile.ban(killerProfile) > 0)
                    enforcePendingBans(killerProfile);
        }

        private void forgiveMyKillerCmd(string victimName)
        {
            forgiveMyKillerCmd(victimName, null);

        }

        private void forgiveMyKillerCmd(string victimName, string killerName)
        {
            if (getActionableViolations(ActionType.forgive).Count == 0)
            {
                SendPlayerMessage(victimName, action2stringING(ActionType.forgive) + " is not allowed");
                return;
            }

            List<PlayerProfile> profiles = getVictimAndKillerProfiles(victimName, killerName, ActionType.forgive);

            if (profiles == null)
                return;

            PlayerProfile victimProfile = profiles[0];
            profiles.RemoveAt(0);

            List<PlayerProfile> killerProfiles = profiles;

            //there is one or more killers under that name
            foreach (PlayerProfile killerProfile in killerProfiles)
                victimProfile.forgive(killerProfile);

        }


        public string violation2stringING(ViolationType violation)
        {
            switch (violation)
            {
                case ViolationType.rape_kill:
                    return "base raping";
                case ViolationType.team_kill:
                    return "team killing";
                case ViolationType.camp_kill:
                    return "base camping";
                default:
                    return "(%violation%)";
            }
        }
        public string playerstate2stringED(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.alive:
                    return "is alive";
                case PlayerState.dead:
                    return "is dead";
                case PlayerState.kicked:
                    return "was kicked";
                case PlayerState.banned:
                    return "was banned";
                case PlayerState.left:
                    return "left the game";
                case PlayerState.limbo:
                    return "is in limbo";
                case PlayerState.violated:
                    return "was violated";
                default:
                    return "(%player_state%)";
            }

        }

        public string action2stringING(ActionType action)
        {
            switch (action)
            {
                case ActionType.ban:
                    return "banning";
                case ActionType.forgive:
                    return "forgiving";
                case ActionType.punish:
                    return "punishing";
                case ActionType.kick:
                    return "kicking";
                default:
                    return "(%action%)";
            }
        }

        public string list2string(List<string> list, string glue)
        {

            if (list == null || list.Count == 0)
                return "";
            else if (list.Count == 1)
                return list[0];

            string last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);

            string str = "";
            foreach (string item in list)
                str += item + ", ";

            return str + glue + last;
        }

        public string list2string(List<string> list)
        {
            return list2string(list, "and ");
        }


        private void playersStatsCmd(string sender)
        {
            SendPlayerMessage(sender, "Players List:");
            int count = 0;
            foreach (KeyValuePair<string, PlayerProfile> pair in this.players)
            {
                count++;
                PlayerProfile playerProfile = pair.Value;
                SendPlayerMessage(sender, count + ". " + playerProfile.ToString());
            }
        }

        private void playerStatsCmd(string sender, string name)
        {

            List<PlayerProfile> list = getPlayersProfile(name);
            if (list.Count == 0)
            {
                SendConsoleMessage(sender, "no player matching \"" + name + "\"");
                return;
            }

            foreach (PlayerProfile killer in list)
            {



                List<PlayerProfile> voters = killer.getVoters();
                List<PlayerProfile> banvoters = killer.getBanVoters();
                List<PlayerProfile> punishers = killer.getQueuedPunishersList();
                List<PlayerProfile> teamKillVictims = killer.getTeamKillVictims();
                List<PlayerProfile> campKillVictims = killer.getCampKillVictims();
                List<PlayerProfile> rapeKillVictims = killer.getRapeKillVictims();


                SendPlayerMessage(sender, "=== " + killer.name + " ( " + killer.state.ToString() + " ) ===");
                SendPlayerMessage(sender, "    Queued Punishments: " + killer.getQueuedPunishmentsCount());
                foreach (PlayerProfile victim in punishers)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (" + killer.getQueuedPunishmentsStr(victim) + ")");
                }

                SendPlayerMessage(sender, "   Kick Votes Received: " + killer.getVotesReceived());
                foreach (PlayerProfile victim in voters)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (" + killer.getVotesReceived(victim) + ")");
                }

                SendPlayerMessage(sender, "   Ban Votes Received: " + killer.getVotesReceived());
                foreach (PlayerProfile victim in banvoters)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (" + killer.getBanVotesReceived(victim) + ")");
                }

                SendPlayerMessage(sender, "    Team Kills: " + killer.getTeamKills());
                foreach (PlayerProfile victim in teamKillVictims)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (Total: " + killer.getTeamKills(victim) + ", Pending: " + killer.getPendingTeamKills(victim) + ") ");
                }

                SendPlayerMessage(sender, "    Base Raping Kills: " + killer.getRapeKills());
                foreach (PlayerProfile victim in rapeKillVictims)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (Total: " + killer.getRapeKills(victim) + ", Pending: " + killer.getPendingRapeKills(victim) + ") ");
                }


                SendPlayerMessage(sender, "    Camping Kills: " + killer.getCampKills());
                foreach (PlayerProfile victim in campKillVictims)
                {
                    SendPlayerMessage(sender, "        " + victim.name + " (Total: " + killer.getCampKills(victim) + ", Pending: " + killer.getPendingCampKills(victim) + ") ");
                }
            }
        }

        private void mkZoneCmd(string sender, string zone_tag)
        {
            System.Guid guid = System.Guid.NewGuid();

            this.selected_zone = new MapZone(guid.ToString(), this.map_file, zone_tag, new Point3D[0], false);
            this.addMapZone(this.selected_zone);
            SendConsoleMessage(sender, "zone " + zone2string(selected_zone) + " added");
        }

        private void readPointsCmd(string sender)
        {
            if (this.reading_points.CompareTo("") != 0)
            {
                SendConsoleMessage(sender, " points being read by " + this.reading_points);
                return;
            }
            else if (this.selected_zone == null)
            {
                SendConsoleMessage(sender, "no zone selected");
                return;
            }

            SendConsoleMessage(sender + ", commit suicide, to designate a point");
            this.reading_points = sender;
        }

        private void closePointsCmd(string sender)
        {
            if (this.reading_points.CompareTo("") == 0)
            {
                SendConsoleMessage(sender, " no points being read");
                return;
            }

            SendConsoleMessage(sender, " stopped reading points");
            this.reading_points = ""; ;
        }

        private void makeRectangleCmd(string sender)
        {

            MapZone zn = this.selected_zone;
            if (zn == null)
            {
                SendConsoleMessage(sender, "no zone selected");
                return;
            }

            Point3D[] points = zn.ZonePolygon;

            if (points.Length != 2)
            {
                SendConsoleMessage(sender, "need two points to make a rectangle");
                return;
            }

            Point3D point1 = points[0];
            Point3D point2 = points[1];
            Point3D point3 = new Point3D(point1.X, point2.Y, 0);
            Point3D point4 = new Point3D(point2.X, point1.Y, 0);


            Point3D[] newPoints = new Point3D[4];
            newPoints[0] = point1;
            newPoints[1] = point4;
            newPoints[2] = point2;
            newPoints[3] = point3;

            zn.ZonePolygon = newPoints;

            SendConsoleMessage(sender, "point " + point2String(point4) + " added");
            SendConsoleMessage(sender, "point " + point2String(point3) + " added");
        }


        private string zone2settings(MapZone zn)
        {
            string str = "procon.protected.zones.add \"";

            str = "procon.protected.zones.add \"" + zn.UID + "\" \"" + zn.LevelFileName + "\" \"" + zn.Tags.ToString() + "\" " + zn.ZonePolygon.Length.ToString();

            foreach (Point3D point in zn.ZonePolygon)
                str += " " + point.X.ToString() + " " + point.Y.ToString() + " " + point.Z.ToString();

            return str;
        }

        private void saveZonesCmd(string sender, string filename)
        {
            string cwd = Directory.GetCurrentDirectory();
            string path = cwd + "/" + filename;

            try
            {
                FileStream fout = System.IO.File.Create(path);
                StreamWriter sw = new StreamWriter(fout);
                List<MapZone> mzns = getCurrentMapZones();

                foreach (MapZone zn in mzns)
                    sw.WriteLine(zone2settings(zn));

                sw.Close();
                fout.Close();

                SendConsoleMessage(sender, mzns.Count + " zones written to " + filename);
            }
            catch (Exception e)
            {
                SendConsoleMessage(sender, "cannot create file " + filename);
                SendConsoleMessage(sender, e.Message);
            }

        }

        private void dropPointCmd(string sender, string strIndex)
        {
            if (this.selected_zone == null)
            {
                SendConsoleMessage(sender, "there is no zone selected");
                return;
            }

            int index = 0;

            if (int.TryParse(strIndex, out index) == false)
            {
                SendConsoleMessage(sender, "invalid point number");
                return;
            }

            if (this.selected_zone.ZonePolygon.Length == 0)
            {
                SendConsoleMessage(sender, "there are no points defined in the current zone.");
                return;
            }

            if (index > this.selected_zone.ZonePolygon.Length || index < 1)
            {
                SendConsoleMessage(sender, "point #" + index + " does not exist");
                return;
            }

            List<Point3D> list = new List<Point3D>(this.selected_zone.ZonePolygon);

            SendConsoleMessage(sender, "point #" + index + " " + point2String(list[index - 1]) + " removed from current zone");

            list.RemoveAt(index - 1);
            this.selected_zone.ZonePolygon = list.ToArray();
        }

        private string zone2string(MapZone zn)
        {
            return zn.UID.ToString() + " " + zn.Tags.ToString();
        }

        private void dropZoneCmd(string sender, string strIndex)
        {
            int index = 0;

            if (int.TryParse(strIndex, out index) == false)
            {
                SendConsoleMessage(sender, "invalid zone number");
                return;
            }

            List<MapZone> zns = getCurrentMapZones();

            if (zns.Count == 0)
            {
                SendConsoleMessage(sender, "there are no zones defined.");
                return;
            }

            if (index > zns.Count || index < 1)
            {
                SendConsoleMessage(sender, "zone #" + index + " does not exist");
                return;
            }

            SendConsoleMessage(sender, "zone #" + index + " " + zone2string(zns[index - 1]) + " dropped");
            if (this.selected_zone == zns[index - 1])
                this.selected_zone = null;
            removeMapZone(index - 1);
        }

        private void llistZonesCmd(string sender)
        {

            List<MapZone> zns = getCurrentMapZones();
            int count = zns.Count;

            if (count == 0)
            {
                SendConsoleMessage(sender, "there are no zones defined.");
                return;
            }

            count = 1;
            foreach (MapZone zn in zns)
            {
                SendConsoleMessage(sender, count + ". " + zone2string(zn));
                count++;
            }
        }

        private void selectZoneCmd(string sender, string zoneNo)
        {
            int index;
            if (int.TryParse(zoneNo, out index) == false)
            {
                SendConsoleMessage(sender, "invalid zone number");
            }

            List<MapZone> zns = getCurrentMapZones();

            if (zns.Count == 0)
            {
                SendConsoleMessage(sender, "there are no zones defined.");
                return;
            }

            if (index > zns.Count || index < 1)
            {
                SendConsoleMessage(sender, "zone #" + index + " does not exist");
                return;
            }

            this.selected_zone = zns[index - 1];
            SendConsoleMessage(sender, "zone #" + index + " " + zone2string(this.selected_zone) + " selected");
        }

        private void listPointsCmd(string sender)
        {
            if (this.selected_zone == null)
            {
                SendConsoleMessage(sender, "there is no zone selected");
                return;
            }


            if (this.selected_zone.ZonePolygon.Length == 0)
            {
                SendConsoleMessage(sender, "there are no points defined in current zone");
                return;
            }

            int count = 0;

            foreach (Point3D point in this.selected_zone.ZonePolygon)
            {
                count++;
                SendConsoleMessage(sender, count + ". " + point2String(point));
            }
        }

        private string point2String(Point3D point)
        {
            return " [ X = " + Decimal.Round(point.X, 2) + ", Y = " + Decimal.Round(point.Y, 2) + ", Z = " + Decimal.Round(point.Z, 2) + " ]";
        }

        private bool isAdmin(string soldier)
        {
            List<string> admin_list = getStringListVarValue("admin_list");
            return admin_list.Contains(soldier);
        }

        private bool isImmune(PlayerProfile player)
        {
            string name = player.name;
            string clan = player.info.ClanTag;

            //List<string> admin_list = getStringListVarValue("admin_list");
            List<string> clan_whitelist = getStringListVarValue("clan_whitelist");
            List<string> player_whitelist = getStringListVarValue("player_whitelist");

            List<string> immune = new List<string>();
            immune.AddRange(player_whitelist);
            //immune.AddRange(admin_list);

            if (immune.Contains(name))
                return true;

            else if (clan != null && clan.Length > 0 && clan_whitelist.Contains(player.info.ClanTag))
                return true;

            return false;
        }

        private List<MapZone> getCurrentMapZones()
        {
            if (this.getBooleanVarValue("default_zones"))
                this.zones = this.default_zones;
            else
                this.zones = this.custom_zones;

            if (!this.zones.ContainsKey(this.map_file))
                this.zones.Add(this.map_file, new List<MapZone>());


            return this.zones[this.map_file];
        }

        public List<MapZone> getAllDefaaultZones()
        {

            List<MapZone> zns = new List<MapZone>();
            ConsoleWrite(" found default zones for " + this.default_zones.Count + " maps");
            foreach (KeyValuePair<string, List<MapZone>> pair in this.default_zones)
            {
                if (pair.Value == null)
                {
                    ConsoleWrite(" no zones in " + pair.Key);
                    continue;
                }

                ConsoleWrite(pair.Value.Count + " zones in " + pair.Key);
                foreach (MapZone zn in pair.Value)
                    zns.Add(zn);
            }
            return zns;

        }

        private bool removeMapZone(int index)
        {
            if (!this.zones.ContainsKey(this.map_file))
                this.zones.Add(this.map_file, new List<MapZone>());

            if (this.zones[this.map_file].Count > index)
            {
                this.zones[this.map_file].RemoveAt(index);
                return true;
            }

            return false;
        }

        private void addMapZone(MapZone zn)
        {

            List<MapZone> current_zones = getCurrentMapZones();

            foreach (MapZone czn in current_zones)
                if (czn.UID.CompareTo(zn.UID) == 0)
                    return;

            current_zones.Add(zn);
        }

        private void addDefaultZone(MapZone zn)
        {

            List<MapZone> current_zones = getMapDefaultZones(zn.LevelFileName);
            foreach (MapZone czn in current_zones)
                if (czn.UID.CompareTo(zn.UID) == 0)
                    return;


            current_zones.Add(zn);
        }



        private List<MapZone> getMapDefaultZones(string map)
        {

            if (!this.default_zones.ContainsKey(map))
                this.default_zones.Add(map, new List<MapZone>());


            return this.default_zones[map];
        }

        private void detectZone(MapZone zn)
        {
            if (!isInsaneZone(zn))
                return;

            List<MapZone> current_zones = getCurrentMapZones();

            foreach (MapZone czn in current_zones)
                if (czn.UID.CompareTo(zn.UID) == 0)
                    return;


            SendConsoleMessage(zone2string(zn) + " detected");
            current_zones.Add(zn);

        }

        private void DefenseKill(string zone, CPlayerInfo defender, CPlayerInfo victim)
        {
            string msg = defender.SoldierName + " defended the " + zone + " base from " + victim.SoldierName;
            SendPlayerMessage(defender.SoldierName, msg);
            SendPlayerMessage(victim.SoldierName, msg);
        }

        private void DefenseDeath(string zone, CPlayerInfo defender, CPlayerInfo killer)
        {

            string msg = defender.SoldierName + " died honorably defending the " + zone + " base from " + killer.SoldierName;
            SendPlayerMessage(defender.SoldierName, msg);
            SendPlayerMessage(killer.SoldierName, msg);

        }

        private void BaseRapingKill(string zone, CPlayerInfo baseraper, CPlayerInfo victim)
        {

            PlayerProfile killerProfile = getPlayerProfile(baseraper);
            PlayerProfile victimProfile = getPlayerProfile(victim);

            if (killerProfile != null && victimProfile != null)
                killerProfile.addRape(victimProfile);

            if (getBooleanVarValue("auto_punish_" + ViolationType.rape_kill.ToString()))
                punishMyKillerCmd(victim.SoldierName);

        }

        private void TeamKill(CPlayerInfo killer, CPlayerInfo victim)
        {

            PlayerProfile killerProfile = getPlayerProfile(killer);
            PlayerProfile victimProfile = getPlayerProfile(victim);

            if (killerProfile != null && victimProfile != null)
                killerProfile.addTk(victimProfile);

            if (getBooleanVarValue("auto_punish_" + ViolationType.team_kill.ToString()))
                punishMyKillerCmd(victim.SoldierName);

        }

        private void BaseCampingKill(string zone, CPlayerInfo camper, CPlayerInfo victim)
        {
            PlayerProfile killerProfile = getPlayerProfile(camper);
            PlayerProfile victimProfile = getPlayerProfile(victim);

            if (killerProfile != null && victimProfile != null)
                killerProfile.addCamp(victimProfile);

            if (getBooleanVarValue("auto_punish_" + ViolationType.camp_kill.ToString()))
                punishMyKillerCmd(victim.SoldierName);
        }

        private void TrespassDeath(string zone, CPlayerInfo trespasser, CPlayerInfo killer)
        {
            string msg = killer.SoldierName + " killed " + trespasser.SoldierName + " for trespassing into the " + zone + " base";
            SendPlayerMessage(killer.SoldierName, msg);
            SendPlayerMessage(trespasser.SoldierName, msg);
        }



        private List<string> getAllowedActionCommands(ViolationType violation)
        {
            List<string> list = new List<string>();

            if (violation.Equals(ViolationType.invalid))
                return list;

            foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
            {
                if (action.Equals(ActionType.invalid))
                    continue;

                string var = getActionViolationVarName(action, violation);
                bool val = getBooleanVarValue(var);

                if (val)
                    list.Add(getActionCommand(action));
            }

            return list;
        }


        private string getViolationInfoMessage(PlayerProfile killer, PlayerProfile victim, ViolationType violation)
        {
            string info_msg = "";
            if (violation.Equals(ViolationType.team_kill))
                info_msg = killer.name + " team killed you!";
            else if (violation.Equals(ViolationType.camp_kill))
                info_msg = killer.name + " killed you while base camping!";
            else if (violation.Equals(ViolationType.rape_kill))
                info_msg = killer.name + " base raped you!";

            return info_msg;

        }

        private void showActionCommands(PlayerProfile victim, PlayerProfile killer, ViolationType violation)
        {

            if (violation.Equals(ViolationType.invalid))
                return;

            string warn_msg = getWarningMessage(killer, victim, violation);
            string info_msg = getViolationInfoMessage(killer, victim, violation);
            string act_msg = getActionCommandsStr(violation);

            //if there is no info message, or action message quit
            if (act_msg.CompareTo("") == 0 || info_msg.CompareTo("") == 0)
                return;

            int yellTime = getIntegerVarValue("yell_" + violation.ToString());
            bool say = getBooleanVarValue("say_" + violation.ToString());
            bool auto_punish = getBooleanVarValue("auto_punish_" + violation.ToString());


            //there are no actions allowed for this voilation

            if (yellTime > 0)
            {
                if (!auto_punish)
                {
                    SendPlayerRespawnYell(victim.name, info_msg, yellTime);
                    SendPlayerRespawnYell(victim.name, act_msg, yellTime);
                }

                //only yell if there are any warnings
                if (warn_msg.CompareTo("") != 0)
                    SendPlayerYell(killer.name, warn_msg, yellTime);
            }

            if (say || auto_punish)
            {
                if (!auto_punish)
                {
                    SendPlayerMessage(victim.name, info_msg);
                    SendPlayerMessage(victim.name, act_msg);
                }

                //only send if there are any warnings
                if (warn_msg.CompareTo("") != 0)
                    SendPlayerMessage(killer.name, warn_msg);
            }
        }

        private string getActionCommandsStr(ViolationType violation)
        {
            List<string> cmds = getAllowedActionCommands(violation);
            string msg = list2string(cmds, "or ");
            if (msg.CompareTo("") == 0)
                return "";
            else
                return "Take action, type:" + msg;
        }


        public string getActionViolationVarName(ActionType action, ViolationType violation)
        {
            return action.ToString() + "_" + violation.ToString();
        }

        public List<ViolationType> getActionableViolations(ActionType action)
        {
            List<ViolationType> list = new List<ViolationType>();

            if (action.Equals(ActionType.invalid))
                return list;

            foreach (ViolationType violation in Enum.GetValues(typeof(ViolationType)))
            {
                if (violation.Equals(ViolationType.invalid))
                    continue;

                string var = getActionViolationVarName(action, violation);
                //SendConsoleMessage("Getting value for: " + var + " and auto_" + var);

                if (getBooleanVarValue(var) || getBooleanVarValue("auto_" + var))
                    list.Add(violation);
            }

            return list;
        }

        public string getActionableViolationsStr(ActionType action)
        {
            List<ViolationType> violations = getActionableViolations(action);

            List<String> list = new List<string>();
            foreach (ViolationType violation in violations)
                list.Add(violation2stringING(violation));

            return list2string(list, "or ");
        }

        private string getActionCommand(ActionType action)
        {
            if (action.Equals(ActionType.forgive))
                return "!forgive";
            else if (action.Equals(ActionType.punish))
                return "!punish";
            else if (action.Equals(ActionType.kick))
                return "!votekick";
            else if (action.Equals(ActionType.ban))
                return "!voteban";

            return "!votekick";
        }



        private List<string> getViolationWarnings(ViolationType violation)
        {
            if (violation.Equals(ViolationType.team_kill))
                return getStringListVarValue("team_kill_warn_list");
            else if (violation.Equals(ViolationType.rape_kill))
                return getStringListVarValue("rape_kill_warn_list");
            else if (violation.Equals(ViolationType.camp_kill))
                return getStringListVarValue("camp_kill_warn_list");
            else
                return new List<string>();
        }

        private string getWarningMessage(PlayerProfile killer, PlayerProfile victim, ViolationType violation)
        {


            List<string> list = getViolationWarnings(violation);
            int count = killer.getViolations(violation);
            int total = list.Count;

            string msg;

            if (list.Count == 0)
                msg = "";
            else if (count >= total)
                msg = list[total - 1];
            else
                msg = list[count - 1];

            //do the replacements

            msg = msg.Replace("%killer%", killer.name);
            msg = msg.Replace("%victim%", victim.name);
            msg = msg.Replace("%count%", count.ToString());

            return msg;
        }


        private PlayerProfile getPlayerProfile(CPlayerInfo info)
        {
            return getPlayerProfile(info.SoldierName);
        }


        private List<PlayerProfile> getPlayersProfile(string name)
        {
            List<PlayerProfile> profiles = new List<PlayerProfile>();
            foreach (KeyValuePair<string, PlayerProfile> pair in this.players)
            {
                if (pair.Key.ToLower().Contains(name.ToLower()))
                    profiles.Add(pair.Value);
            }

            return profiles;
        }

        private PlayerProfile getPlayerProfile(string name)
        {
            PlayerProfile pp;
            this.players.TryGetValue(name, out pp);
            return pp;
        }




        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

            if (cpbiPlayer != null)
            {
                if (this.players.ContainsKey(cpbiPlayer.SoldierName))
                    this.players[cpbiPlayer.SoldierName].pbinfo = cpbiPlayer;
                else
                    this.players.Add(cpbiPlayer.SoldierName, new PlayerProfile(this, cpbiPlayer));
            }
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.players.ContainsKey(cpiPlayer.SoldierName))
                        this.players[cpiPlayer.SoldierName].info = cpiPlayer;
                    else
                        this.players.Add(cpiPlayer.SoldierName, new PlayerProfile(this, cpiPlayer));
                }
            }

        }

    }
}