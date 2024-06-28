drop table if exists symbols;
create table symbols
(
    id              integer     primary key,
    name            text        default null,
    type            text        not null,
    filename        text        not null,
    namespace       text        default null
);

drop table if exists classes;
create table classes
(
    id              integer     primary key,
    name            text        not null,
    parent_symbol   integer     default null,

    foreign key(parent_symbol)
      references symbols(id)
);

drop table if exists variables;
create table variables
(
    id              integer     primary key,
    name            text        not null,
    anchor_file     text        default null,
    anchor          text        default null,
    arg_list        text        default null,
    type            text        default null,
    parent_symbol   integer     default null,

    foreign key(parent_symbol)
      references symbols(id)
);

drop table if exists functions;
create table functions
(
    id              integer     primary key,
    name            text        not null,
    anchor_file     text        default null,
    anchor          text        default null,
    arg_list        text        default null,
    type            text        default null,
    parent_symbol   integer     default null,

    foreign key(parent_symbol)
      references symbols(id)
);

-- Index columns that will be used for lookups
create index symbols_filename_idx
    on symbols(filename);
create index symbols_type_idx
    on symbols(type);
