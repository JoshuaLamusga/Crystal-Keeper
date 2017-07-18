using CrystalKeeper.Core;
using System;
using System.Windows.Controls;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a project.
    /// </summary>
    class PgCollectionEdit
    {
        #region Members
        /// <summary>
        /// The database item associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgCollectionGuiEdit gui;

        /// <summary>
        /// The collection to be used.
        /// </summary>
        private DataItem collection;

        /// <summary>
        /// Fires when project data is changed.
        /// </summary>
        public event EventHandler DataNameChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the data used to construct the page.
        /// </summary>
        public Project Project
        {
            set
            {
                project = value;
                ConstructPage();
            }
            get
            {
                return project;
            }
        }

        /// <summary>
        /// Gets or sets the gui that controls the page appearance.
        /// </summary>
        public PgCollectionGuiEdit Gui
        {
            private set
            {
                gui = value;
            }
            get
            {
                return gui;
            }
        }

        /// <summary>
        /// The collection to be used.
        /// </summary>
        public DataItem Collection
        {
            set
            {
                collection = value;
            }
            get
            {
                return collection;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a collection page from the given project.
        /// </summary>
        /// <param name="project">
        /// The project given.
        /// </param>
        /// <param name="collection">
        /// The collection being edited.
        /// </param>
        public PgCollectionEdit(Project project, DataItem collection)
        {
            this.project = project;
            this.collection = collection;
            ConstructPage();
        }

        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgCollectionGuiEdit();

            #region Collection name
            //Sets the collection name.
            if (string.IsNullOrWhiteSpace((string)collection.GetData("name")))
            {
                gui.TxtblkCollectionName.Text = "Untitled";
            }
            else
            {
                gui.TxtblkCollectionName.Text = (string)collection.GetData("name");
            }

            //Handles changes to the database name.
            gui.TxtblkCollectionName.TextChanged += new TextChangedEventHandler((a, b) =>
            {
                if (!string.IsNullOrWhiteSpace(gui.TxtblkCollectionName.Text))
                {
                    collection.SetData("name", gui.TxtblkCollectionName.Text);
                }

                //If the textbox is empty, it will keep the last character.
                gui.TxtblkCollectionName.Text = (string)collection.GetData("name");

                DataNameChanged?.Invoke(this, null);
            });
            #endregion

            #region Template name
            gui.TxtblkTemplateName.Text = "Template: ";
            //Sets the template name.
            if (string.IsNullOrWhiteSpace((string)project
                .GetCollectionTemplate(collection).GetData("name")))
            {
                gui.TxtblkTemplateName.Text += "Untitled";
            }
            else
            {
                gui.TxtblkTemplateName.Text += (string)project
                .GetCollectionTemplate(collection).GetData("name");
            }
            #endregion

            #region Description
            //Sets the description.
            gui.TxtbxDescription.Text = (string)collection.GetData("description");

            //Handles changes to the collection description.
            gui.TxtbxDescription.TextChanged += new TextChangedEventHandler((a, b) =>
                {
                    collection.SetData("description", gui.TxtbxDescription.Text);
                });
            #endregion
        }
        #endregion
    }
}